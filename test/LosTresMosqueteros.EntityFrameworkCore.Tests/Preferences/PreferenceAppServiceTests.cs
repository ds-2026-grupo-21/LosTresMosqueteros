using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LosTresMosqueteros.Preferences;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Security.Claims;
using Volo.Abp.Validation;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class PreferenceAppServiceTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private readonly IPreferenceAppService _service;
    private readonly IRepository<UserPreference, Guid> _preferences;
    private readonly ICurrentPrincipalAccessor _principal;

    public PreferenceAppServiceTests()
    {
        _service = GetRequiredService<IPreferenceAppService>();
        _preferences = GetRequiredService<IRepository<UserPreference, Guid>>();
        _principal = GetRequiredService<ICurrentPrincipalAccessor>();
    }

    private IDisposable AsUser(Guid id) => _principal.Change(new ClaimsPrincipal(
        new ClaimsIdentity(new[] { new Claim(AbpClaimTypes.UserId, id.ToString()) }, "Test")));

    [Fact]
    public async Task Sin_datos_previos_devuelve_los_valores_iniciales_sin_crear_filas()
    {
        var userId = Guid.NewGuid();
        using (AsUser(userId))
        {
            var result = await _service.GetAsync();
            result.ShowAlerts.ShouldBeTrue();
            result.WatchedItems.ShouldBeEmpty();
        }

        (await WithUnitOfWorkAsync(() => _preferences.GetListAsync(x => x.UserId == userId))).ShouldBeEmpty();
    }

    [Fact]
    public async Task Guarda_recupera_y_evita_repetidos_sin_importar_mayusculas_ni_acentos()
    {
        using var user = AsUser(Guid.NewGuid());

        var saved = await _service.UpdateAsync(new UpdateUserPreferenceDto
        {
            ShowAlerts = false,
            WatchedItems = new List<string?> { "Maní", "mani ", "Gluten" }
        });

        saved.ShowAlerts.ShouldBeFalse();
        saved.WatchedItems.Count.ShouldBe(2);

        var loaded = await _service.GetAsync();
        loaded.ShowAlerts.ShouldBeFalse();
        loaded.WatchedItems.ShouldBe(new[] { "Gluten", "Maní" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Conserva_textos_distintos_y_deduplica_mayusculas_y_acentos()
    {
        using var user = AsUser(Guid.NewGuid());
        await _service.UpdateAsync(new UpdateUserPreferenceDto
        {
            WatchedItems = new() { "œuf", "oeuf", "Maní", "MANI" }
        });
        (await _service.GetAsync()).WatchedItems.ShouldBe(new[] { "œuf", "oeuf", "Maní" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Actualizar_de_nuevo_agrega_y_quita_entradas_sobre_la_misma_fila()
    {
        var userId = Guid.NewGuid();
        using var user = AsUser(userId);

        await _service.UpdateAsync(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { "Gluten", "Maní" } });
        await _service.UpdateAsync(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { "Gluten", "Lactosa" } });

        (await _service.GetAsync()).WatchedItems.ShouldBe(new[] { "Gluten", "Lactosa" }, ignoreOrder: true);
        (await WithUnitOfWorkAsync(() => _preferences.GetListAsync(x => x.UserId == userId))).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Una_lista_vacia_borra_las_entradas_y_conserva_la_opcion_de_alertas()
    {
        using var user = AsUser(Guid.NewGuid());

        await _service.UpdateAsync(new UpdateUserPreferenceDto { ShowAlerts = false, WatchedItems = new List<string?> { "Gluten" } });
        var emptied = await _service.UpdateAsync(new UpdateUserPreferenceDto { ShowAlerts = false, WatchedItems = new List<string?>() });

        emptied.WatchedItems.ShouldBeEmpty();
        (await _service.GetAsync()).ShowAlerts.ShouldBeFalse();
    }

    [Fact]
    public async Task Cada_usuario_ve_y_modifica_solo_sus_preferencias()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        using (AsUser(userA))
            await _service.UpdateAsync(new UpdateUserPreferenceDto { ShowAlerts = false, WatchedItems = new List<string?> { "Gluten" } });

        using (AsUser(userB))
        {
            var initial = await _service.GetAsync();
            initial.ShowAlerts.ShouldBeTrue();
            initial.WatchedItems.ShouldBeEmpty();

            await _service.UpdateAsync(new UpdateUserPreferenceDto { ShowAlerts = true, WatchedItems = new List<string?> { "Lactosa" } });
        }

        using (AsUser(userA))
        {
            var a = await _service.GetAsync();
            a.ShowAlerts.ShouldBeFalse();
            a.WatchedItems.ShouldBe(new[] { "Gluten" });
        }

        using (AsUser(userB))
            (await _service.GetAsync()).WatchedItems.ShouldBe(new[] { "Lactosa" });
    }

    [Fact]
    public async Task El_mismo_texto_puede_estar_en_dos_usuarios()
    {
        using (AsUser(Guid.NewGuid()))
            await _service.UpdateAsync(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { "Gluten" } });

        using (AsUser(Guid.NewGuid()))
        {
            var result = await _service.UpdateAsync(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { "gluten" } });
            result.WatchedItems.Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Una_entrada_vacia_se_rechaza_y_no_cambia_lo_guardado()
    {
        using var user = AsUser(Guid.NewGuid());
        await _service.UpdateAsync(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { "Gluten" } });

        await Should.ThrowAsync<AbpValidationException>(() =>
            _service.UpdateAsync(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { "  " } }));

        (await _service.GetAsync()).WatchedItems.ShouldBe(new[] { "Gluten" });
    }

    [Fact]
    public void El_dto_rechaza_entradas_vacias_largas_y_demasiadas()
    {
        static bool IsValid(UpdateUserPreferenceDto dto) =>
            Validator.TryValidateObject(dto, new ValidationContext(dto), new List<ValidationResult>(), true);

        IsValid(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { "Gluten" } }).ShouldBeTrue();
        IsValid(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { " " } }).ShouldBeFalse();
        IsValid(new UpdateUserPreferenceDto { WatchedItems = new List<string?> { null } }).ShouldBeFalse();
        IsValid(new UpdateUserPreferenceDto
        {
            WatchedItems = new List<string?> { new string('a', PreferenceConsts.MaxNameLength + 1) }
        }).ShouldBeFalse();
        IsValid(new UpdateUserPreferenceDto
        {
            WatchedItems = Enumerable.Range(0, PreferenceConsts.MaxWatchedItems + 1).Select(i => (string?)$"item {i}").ToList()
        }).ShouldBeFalse();
    }
}
