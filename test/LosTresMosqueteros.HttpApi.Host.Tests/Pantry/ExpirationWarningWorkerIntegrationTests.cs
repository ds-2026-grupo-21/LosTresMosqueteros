using System;
using System.Linq;
using System.Threading.Tasks;
using LosTresMosqueteros.EntityFrameworkCore;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Productos;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Xunit;

namespace LosTresMosqueteros;

/// <summary>
/// Ejecuta el ExpirationWarningWorker real (con su manager, repositorios y base de pruebas)
/// sin esperar el intervalo del temporizador. El reloj se controla para fijar el día UTC.
/// </summary>
public class ExpirationWarningWorkerIntegrationTests
    : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private static readonly DateOnly TodayUtc = new(2026, 10, 21);
    private static readonly DateTime NoonUtc = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;

    public ExpirationWarningWorkerIntegrationTests()
    {
        _items = GetRequiredService<IRepository<PantryItem, Guid>>();
        _warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
    }

    [Fact]
    public async Task Dos_ejecuciones_crean_avisos_solo_dentro_del_umbral_y_no_los_duplican()
    {
        var expired = await CreateItemAsync(TodayUtc.AddDays(-4));
        var today = await CreateItemAsync(TodayUtc);
        var limit = await CreateItemAsync(TodayUtc.AddDays(3));
        var outside = await CreateItemAsync(TodayUtc.AddDays(4));
        var withoutDate = await CreateItemAsync(null);
        var consumed = await CreateItemAsync(TodayUtc.AddDays(1), consumed: true);

        await RunWorkerAsync(NoonUtc);

        (await CountWarningsAsync()).ShouldBe(3);
        var first = new[]
        {
            (await ReadWarningAsync(expired)).ShouldNotBeNull(),
            (await ReadWarningAsync(today)).ShouldNotBeNull(),
            (await ReadWarningAsync(limit)).ShouldNotBeNull()
        };
        first.ShouldAllBe(x => x.IsActive);
        first[0].DaysRemaining.ShouldBe(-4);
        first[1].DaysRemaining.ShouldBe(0);
        first[2].DaysRemaining.ShouldBe(3);
        (await ReadWarningAsync(outside)).ShouldBeNull();
        (await ReadWarningAsync(withoutDate)).ShouldBeNull();
        (await ReadWarningAsync(consumed)).ShouldBeNull();

        await RunWorkerAsync(NoonUtc);

        (await CountWarningsAsync()).ShouldBe(3);
        (await ReadWarningAsync(expired)).ShouldNotBeNull().Id.ShouldBe(first[0].Id);
        (await ReadWarningAsync(today)).ShouldNotBeNull().Id.ShouldBe(first[1].Id);
        (await ReadWarningAsync(limit)).ShouldNotBeNull().Id.ShouldBe(first[2].Id);
    }

    [Fact]
    public async Task Entre_ejecuciones_actualiza_desactiva_y_reactiva_la_misma_fila()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));

        await RunWorkerAsync(NoonUtc);
        var original = (await ReadWarningAsync(id)).ShouldNotBeNull();
        original.IsActive.ShouldBeTrue();

        await ChangeItemAsync(id, item => item.SetExpirationDate(TodayUtc.AddDays(2)));
        await RunWorkerAsync(NoonUtc);
        var updated = (await ReadWarningAsync(id)).ShouldNotBeNull();
        updated.Id.ShouldBe(original.Id);
        updated.IsActive.ShouldBeTrue();
        updated.DaysRemaining.ShouldBe(2);

        await ChangeItemAsync(id, item => item.SetExpirationDate(TodayUtc.AddDays(10)));
        await RunWorkerAsync(NoonUtc);
        var deactivated = (await ReadWarningAsync(id)).ShouldNotBeNull();
        deactivated.Id.ShouldBe(original.Id);
        deactivated.IsActive.ShouldBeFalse();

        await ChangeItemAsync(id, item => item.SetExpirationDate(TodayUtc.AddDays(3)));
        await RunWorkerAsync(NoonUtc);
        var reactivated = (await ReadWarningAsync(id)).ShouldNotBeNull();
        reactivated.Id.ShouldBe(original.Id);
        reactivated.IsActive.ShouldBeTrue();
        reactivated.DaysRemaining.ShouldBe(3);

        await ChangeItemAsync(id, item => item.MarkConsumed());
        await RunWorkerAsync(NoonUtc);
        var afterConsumed = (await ReadWarningAsync(id)).ShouldNotBeNull();
        afterConsumed.Id.ShouldBe(original.Id);
        afterConsumed.IsActive.ShouldBeFalse();

        (await CountWarningsAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Usa_el_dia_utc_del_reloj_y_lo_actualiza_al_avanzar_la_fecha()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(4));

        // 21:30 del 21/10 en Argentina = 00:30 del 22/10 en UTC: ya está dentro del umbral.
        await RunWorkerAsync(new DateTime(2026, 10, 22, 0, 30, 0, DateTimeKind.Utc));
        var first = (await ReadWarningAsync(id)).ShouldNotBeNull();
        first.IsActive.ShouldBeTrue();
        first.DaysRemaining.ShouldBe(3);

        // Cuatro días después ya venció: sigue activo, con días negativos, en la misma fila.
        await RunWorkerAsync(new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc));
        var later = (await ReadWarningAsync(id)).ShouldNotBeNull();
        later.Id.ShouldBe(first.Id);
        later.IsActive.ShouldBeTrue();
        later.DaysRemaining.ShouldBe(-1);

        (await CountWarningsAsync()).ShouldBe(1);
    }

    private async Task RunWorkerAsync(DateTime utcNow)
    {
        var clock = Substitute.For<IClock>();
        clock.Now.Returns(utcNow);

        // Cada ejecución usa un alcance nuevo, como el temporizador real.
        using var scope = ServiceProvider.CreateScope();
        var worker = new TestWorker(
            GetRequiredService<IServiceScopeFactory>(),
            new AbpLazyServiceProvider(ServiceProvider));

        await worker.RunOnceAsync(new ClockOverrideProvider(scope.ServiceProvider, clock));
    }

    private async Task<Guid> CreateItemAsync(DateOnly? date, bool consumed = false)
    {
        var id = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            var products = GetRequiredService<IRepository<Producto, Guid>>();
            var product = await products.FindAsync(x => x.CodigoBarras == "77900099")
                ?? await products.InsertAsync(new Producto(Guid.NewGuid(), "77900099", "Producto del worker"), autoSave: true);

            var item = new PantryItem(id, Guid.NewGuid(), product.Id, date);
            if (consumed)
            {
                item.MarkConsumed();
            }
            await _items.InsertAsync(item);
        });
        return id;
    }

    private Task ChangeItemAsync(Guid id, Action<PantryItem> change) =>
        WithUnitOfWorkAsync(async () =>
        {
            var item = await _items.GetAsync(id);
            change(item);
            await _items.UpdateAsync(item);
        });

    private Task<ExpirationWarning?> ReadWarningAsync(Guid itemId) =>
        WithUnitOfWorkAsync(() => _warnings.FindAsync(x => x.PantryItemId == itemId && x.Type == WarningType.Expiration));

    private Task<long> CountWarningsAsync() =>
        WithUnitOfWorkAsync(() => _warnings.GetCountAsync());

    /// <summary>Devuelve el reloj controlado y delega el resto en el alcance real.</summary>
    private sealed class ClockOverrideProvider : IServiceProvider
    {
        private readonly IServiceProvider _inner;
        private readonly IClock _clock;

        public ClockOverrideProvider(IServiceProvider inner, IClock clock)
        {
            _inner = inner;
            _clock = clock;
        }

        public object? GetService(Type serviceType) =>
            serviceType == typeof(IClock) ? _clock : _inner.GetService(serviceType);
    }

    private sealed class TestWorker : ExpirationWarningWorker
    {
        public TestWorker(IServiceScopeFactory scopeFactory, IAbpLazyServiceProvider lazyServiceProvider)
            : base(new AbpAsyncTimer(), scopeFactory)
        {
            LazyServiceProvider = lazyServiceProvider;
        }

        public Task RunOnceAsync(IServiceProvider provider) =>
            DoWorkAsync(new PeriodicBackgroundWorkerContext(provider));
    }
}