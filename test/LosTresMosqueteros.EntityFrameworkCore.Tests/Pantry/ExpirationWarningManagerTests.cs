using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Productos;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class ExpirationWarningManagerTests
    : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private static readonly DateOnly TodayUtc = new(2026, 10, 21);
    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;
    private readonly ExpirationWarningManager _manager;

    public ExpirationWarningManagerTests()
    {
        _items = GetRequiredService<IRepository<PantryItem, Guid>>();
        _warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
        _manager = GetRequiredService<ExpirationWarningManager>();
    }

    [Theory]
    [InlineData(-4)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Deberia_Advertir_Dentro_Del_Umbral_Incluidos_Hoy_Limite_Y_Vencidos(int days)
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(days));

        await _manager.ProcessAsync(TodayUtc);

        var warning = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        warning.IsActive.ShouldBeTrue();
        warning.Type.ShouldBe(WarningType.Expiration);
        warning.ExpirationDate.ShouldBe(TodayUtc.AddDays(days));
        warning.DaysRemaining.ShouldBe(days);
    }

    [Fact]
    public async Task Deberia_Excluir_Fecha_Fuera_Del_Umbral()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(4));
        await _manager.ProcessAsync(TodayUtc);
        (await ReadWarningsAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Deberia_Excluir_Item_Sin_Fecha()
    {
        var id = await CreateItemAsync(null);
        await _manager.ProcessAsync(TodayUtc);
        (await ReadWarningsAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Deberia_Excluir_Item_Consumido()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1), consumed: true);
        await _manager.ProcessAsync(TodayUtc);
        (await ReadWarningsAsync(id)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Deberia_Conservar_Id_Al_Ejecutarse_Dos_Veces()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));
        await _manager.ProcessAsync(TodayUtc);
        var original = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();

        await _manager.ProcessAsync(TodayUtc);

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.Id.ShouldBe(original.Id);
        actual.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Deberia_Actualizar_La_Misma_Advertencia_Al_Cambiar_La_Fecha()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));
        await _manager.ProcessAsync(TodayUtc);
        var original = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        await ChangeItemAsync(id, item => item.SetExpirationDate(TodayUtc.AddDays(3)));

        await _manager.ProcessAsync(TodayUtc);

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.Id.ShouldBe(original.Id);
        actual.ExpirationDate.ShouldBe(TodayUtc.AddDays(3));
        actual.DaysRemaining.ShouldBe(3);
        actual.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Deberia_Actualizar_Y_Desactivar_Al_Alejar_La_Fecha()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));
        await _manager.ProcessAsync(TodayUtc);
        var original = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        await ChangeItemAsync(id, item => item.SetExpirationDate(TodayUtc.AddDays(10)));

        await _manager.ProcessAsync(TodayUtc);

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.Id.ShouldBe(original.Id);
        actual.ExpirationDate.ShouldBe(TodayUtc.AddDays(10));
        actual.DaysRemaining.ShouldBe(10);
        actual.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Deberia_Desactivar_Al_Quitar_La_Fecha_Conservando_La_Ultima_Conocida()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));
        await _manager.ProcessAsync(TodayUtc);
        var original = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        await ChangeItemAsync(id, item => item.SetExpirationDate(null));

        await _manager.ProcessAsync(TodayUtc);

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.Id.ShouldBe(original.Id);
        actual.ExpirationDate.ShouldBe(original.ExpirationDate);
        actual.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Deberia_Desactivar_La_Advertencia_Al_Consumir_El_Item()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));
        await _manager.ProcessAsync(TodayUtc);
        var original = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        await ChangeItemAsync(id, item => item.MarkConsumed());

        await _manager.ProcessAsync(TodayUtc);

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.Id.ShouldBe(original.Id);
        actual.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Deberia_Reactivar_La_Misma_Fila_Al_Volver_Al_Umbral()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));
        await _manager.ProcessAsync(TodayUtc);
        var original = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        await ChangeItemAsync(id, item => item.SetExpirationDate(TodayUtc.AddDays(10)));
        await _manager.ProcessAsync(TodayUtc);
        (await ReadWarningsAsync(id)).ShouldHaveSingleItem().IsActive.ShouldBeFalse();
        await ChangeItemAsync(id, item => item.SetExpirationDate(TodayUtc.AddDays(2)));

        await _manager.ProcessAsync(TodayUtc);

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.Id.ShouldBe(original.Id);
        actual.IsActive.ShouldBeTrue();
        actual.DaysRemaining.ShouldBe(2);
    }

    [Fact]
    public async Task Deberia_Procesar_Items_De_Distintos_Propietarios_Sin_Sesion_Http()
    {
        var first = await CreateItemAsync(TodayUtc.AddDays(1), ownerId: Guid.NewGuid());
        var second = await CreateItemAsync(TodayUtc.AddDays(2), ownerId: Guid.NewGuid());

        await _manager.ProcessAsync(TodayUtc);

        (await ReadWarningsAsync(first)).ShouldHaveSingleItem().PantryItemId.ShouldBe(first);
        (await ReadWarningsAsync(second)).ShouldHaveSingleItem().PantryItemId.ShouldBe(second);
    }

    [Fact]
    public async Task Deberia_Actualizar_Dias_Restantes_Al_Cambiar_El_Dia_De_Referencia()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(3));
        await _manager.ProcessAsync(TodayUtc);
        var original = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();

        await _manager.ProcessAsync(TodayUtc.AddDays(1));

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.Id.ShouldBe(original.Id);
        actual.DaysRemaining.ShouldBe(2);
    }

    [Fact]
    public async Task Deberia_Recuperar_La_Colision_Entre_Dos_Ejecuciones_Con_Unidades_Independientes()
    {
        var id = await CreateItemAsync(TodayUtc.AddDays(1));
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readers = 0;
        var warnings = Substitute.For<IRepository<ExpirationWarning, Guid>>();
        warnings.FindAsync(Arg.Any<Expression<Func<ExpirationWarning, bool>>>(), false, Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var result = await _warnings.FindAsync(
                    call.ArgAt<Expression<Func<ExpirationWarning, bool>>>(0), false,
                    call.ArgAt<CancellationToken>(2));
                if (result is null && Interlocked.Increment(ref readers) <= 2)
                {
                    if (Volatile.Read(ref readers) == 2)
                    {
                        bothRead.TrySetResult();
                    }
                    await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }
                return result;
            });
        warnings.InsertAsync(Arg.Any<ExpirationWarning>(), false, Arg.Any<CancellationToken>())
            .Returns(call => _warnings.InsertAsync(call.ArgAt<ExpirationWarning>(0), false,
                call.ArgAt<CancellationToken>(2)));
        warnings.UpdateAsync(Arg.Any<ExpirationWarning>(), false, Arg.Any<CancellationToken>())
            .Returns(call => _warnings.UpdateAsync(call.ArgAt<ExpirationWarning>(0), false,
                call.ArgAt<CancellationToken>(2)));

        var first = CreateManager(warnings);
        var second = CreateManager(warnings);
        await Task.WhenAll(first.ProcessAsync(TodayUtc), second.ProcessAsync(TodayUtc));

        var actual = (await ReadWarningsAsync(id)).ShouldHaveSingleItem();
        actual.IsActive.ShouldBeTrue();
        actual.DaysRemaining.ShouldBe(1);
        await warnings.Received(3).FindAsync(Arg.Any<Expression<Func<ExpirationWarning, bool>>>(), false,
            Arg.Any<CancellationToken>());
    }

    private ExpirationWarningManager CreateManager(IRepository<ExpirationWarning, Guid> warnings)
    {
        return new ExpirationWarningManager(_items, warnings, GetRequiredService<IUnitOfWorkManager>())
        {
            LazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>()
        };
    }

    private async Task<Guid> CreateItemAsync(DateOnly? date, bool consumed = false, Guid? ownerId = null)
    {
        var id = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
        {
            var products = GetRequiredService<IRepository<Producto, Guid>>();
            var product = await products.InsertAsync(new Producto(Guid.NewGuid(), "7790000000001", "Producto de prueba"));
            var item = new PantryItem(id, ownerId ?? Guid.NewGuid(), product.Id, date);
            if (consumed)
            {
                item.MarkConsumed();
            }
            await _items.InsertAsync(item);
        });
        return id;
    }

    private Task ChangeItemAsync(Guid id, Action<PantryItem> change)
    {
        return WithUnitOfWorkAsync(async () =>
        {
            var item = await _items.GetAsync(id);
            change(item);
            await _items.UpdateAsync(item);
        });
    }

    private Task<List<ExpirationWarning>> ReadWarningsAsync(Guid id)
    {
        return WithUnitOfWorkAsync(() => _warnings.GetListAsync(x => x.PantryItemId == id));
    }
}
