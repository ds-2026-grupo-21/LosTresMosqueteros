using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Productos;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Volo.Abp.Timing;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class PantryExpirationStateTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private readonly IPantryAppService _service;
    private readonly IRepository<Producto, Guid> _products;
    private readonly IRepository<ExpirationWarning, Guid> _warnings;
    private readonly ExpirationWarningManager _manager;
    private readonly ICurrentPrincipalAccessor _principal;
    private readonly DateOnly _todayUtc;

    public PantryExpirationStateTests()
    {
        _service = GetRequiredService<IPantryAppService>();
        _products = GetRequiredService<IRepository<Producto, Guid>>();
        _warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
        _manager = GetRequiredService<ExpirationWarningManager>();
        _principal = GetRequiredService<ICurrentPrincipalAccessor>();
        // Mismo día de referencia que usa el AppService: UTC.
        _todayUtc = DateOnly.FromDateTime(GetRequiredService<IClock>().Now.ToUniversalTime());
    }

    private IDisposable AsUser(Guid id) => _principal.Change(new ClaimsPrincipal(
        new ClaimsIdentity(new[] { new Claim(AbpClaimTypes.UserId, id.ToString()) }, "Test")));

    [Theory]
    [InlineData(-4, ExpirationState.Expired)]
    [InlineData(0, ExpirationState.DueToday)]
    [InlineData(1, ExpirationState.DueSoon)]
    [InlineData(3, ExpirationState.DueSoon)]
    [InlineData(4, ExpirationState.None)]
    public async Task El_listado_informa_el_estado_segun_la_regla_de_tres_dias_utc(int days, ExpirationState expected)
    {
        var productId = await CreateProductAsync("77900001");
        using var user = AsUser(Guid.NewGuid());

        var created = await AddAsync(productId, _todayUtc.AddDays(days));
        created.ExpirationState.ShouldBe(expected);
        created.DaysRemaining.ShouldBe(days);

        var listed = (await _service.GetListAsync(new PagedResultRequestDto())).Items.ShouldHaveSingleItem();
        listed.ExpirationState.ShouldBe(expected);
        listed.DaysRemaining.ShouldBe(days);
    }

    [Fact]
    public async Task Sin_fecha_o_consumido_no_tiene_aviso_activo()
    {
        var productId = await CreateProductAsync("77900002");
        using var user = AsUser(Guid.NewGuid());

        var withoutDate = await AddAsync(productId, null);
        withoutDate.ExpirationState.ShouldBe(ExpirationState.None);
        withoutDate.DaysRemaining.ShouldBeNull();

        var consumed = await AddAsync(productId, _todayUtc.AddDays(1));
        consumed.ExpirationState.ShouldBe(ExpirationState.DueSoon);
        await _service.MarkConsumedAsync(consumed.Id);

        (await _service.GetAsync(consumed.Id)).ExpirationState.ShouldBe(ExpirationState.None);
    }

    [Fact]
    public async Task Un_cambio_de_fecha_se_refleja_sin_esperar_al_worker()
    {
        var productId = await CreateProductAsync("77900003");
        using var user = AsUser(Guid.NewGuid());

        var item = await AddAsync(productId, _todayUtc.AddDays(1));
        await _manager.ProcessAsync(_todayUtc);
        (await ReadWarningAsync(item.Id)).ShouldNotBeNull().IsActive.ShouldBeTrue();

        // El aviso persistido todavía está activo, pero el estado sigue al dato actual del ítem.
        var updated = await _service.UpdateAsync(item.Id, new UpdatePantryItemDto { ExpirationDate = _todayUtc.AddDays(10) });
        updated.ExpirationState.ShouldBe(ExpirationState.None);
        (await _service.GetAsync(item.Id)).ExpirationState.ShouldBe(ExpirationState.None);

        // En la siguiente ejecución, el aviso persistido coincide con el estado.
        await _manager.ProcessAsync(_todayUtc);
        (await ReadWarningAsync(item.Id)).ShouldNotBeNull().IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task El_estado_coincide_con_los_avisos_activos_que_persiste_el_worker()
    {
        var productId = await CreateProductAsync("77900004");
        using var user = AsUser(Guid.NewGuid());

        foreach (var days in new int?[] { -2, 0, 2, 3, 4, 10, null })
        {
            await AddAsync(productId, days.HasValue ? _todayUtc.AddDays(days.Value) : null);
        }
        var consumed = await AddAsync(productId, _todayUtc.AddDays(1));
        await _service.MarkConsumedAsync(consumed.Id);

        await _manager.ProcessAsync(_todayUtc);

        var items = (await _service.GetListAsync(new PagedResultRequestDto { MaxResultCount = 50 })).Items;
        items.Count.ShouldBe(8);
        foreach (var item in items)
        {
            var hasActiveWarning = (await ReadWarningAsync(item.Id))?.IsActive == true;
            hasActiveWarning.ShouldBe(
                item.ExpirationState != ExpirationState.None,
                $"Ítem con {item.DaysRemaining?.ToString() ?? "sin fecha"} días, estado {item.Status}");
        }
    }

    [Fact]
    public async Task Dos_usuarios_con_el_mismo_producto_ven_solo_sus_items_y_estados()
    {
        var productId = await CreateProductAsync("77900005");
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        PantryItemDto itemA;
        PantryItemDto itemB;
        using (AsUser(userA)) itemA = await AddAsync(productId, _todayUtc.AddDays(1));
        using (AsUser(userB)) itemB = await AddAsync(productId, _todayUtc.AddDays(-1));

        await _manager.ProcessAsync(_todayUtc);

        using (AsUser(userA))
        {
            var mine = (await _service.GetListAsync(new PagedResultRequestDto())).Items.ShouldHaveSingleItem();
            mine.Id.ShouldBe(itemA.Id);
            mine.ExpirationState.ShouldBe(ExpirationState.DueSoon);

            await Should.ThrowAsync<EntityNotFoundException>(() => _service.GetAsync(itemB.Id));
            await Should.ThrowAsync<EntityNotFoundException>(() => _service.MarkConsumedAsync(itemB.Id));
        }

        using (AsUser(userB))
        {
            var mine = (await _service.GetListAsync(new PagedResultRequestDto())).Items.ShouldHaveSingleItem();
            mine.Id.ShouldBe(itemB.Id);
            mine.ExpirationState.ShouldBe(ExpirationState.Expired);
            mine.DaysRemaining.ShouldBe(-1);
        }

        // El intento de B sobre A no tocó nada: los dos avisos siguen activos.
        (await ReadWarningAsync(itemA.Id)).ShouldNotBeNull().IsActive.ShouldBeTrue();
        (await ReadWarningAsync(itemB.Id)).ShouldNotBeNull().IsActive.ShouldBeTrue();
    }

    private async Task<Guid> CreateProductAsync(string barcode)
    {
        var id = Guid.NewGuid();
        await WithUnitOfWorkAsync(() => _products.InsertAsync(new Producto(id, barcode, "Producto " + barcode)));
        return id;
    }

    private Task<PantryItemDto> AddAsync(Guid productId, DateOnly? date) =>
        _service.CreateAsync(new CreatePantryItemDto { ProductId = productId, ExpirationDate = date });

    private Task<ExpirationWarning?> ReadWarningAsync(Guid itemId) =>
        WithUnitOfWorkAsync(() => _warnings.FindAsync(x => x.PantryItemId == itemId && x.Type == WarningType.Expiration));
}