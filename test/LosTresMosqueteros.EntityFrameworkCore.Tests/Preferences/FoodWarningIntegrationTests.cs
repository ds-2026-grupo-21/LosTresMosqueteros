using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Preferences;
using LosTresMosqueteros.Productos;
using NSubstitute;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Security.Claims;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

/// <summary>
/// Integración con preferencias persistidas, evaluador real y proveedor externo simulado.
/// Verifica que abrir, guardar y agregar usan la misma regla con las preferencias del usuario actual.
/// </summary>
public class FoodWarningIntegrationTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private const string Codigo = "0000000001001";
    private readonly IPreferenceAppService _preferences;
    private readonly ICurrentPrincipalAccessor _principal;
    private readonly IProductoRepository _products;

    public FoodWarningIntegrationTests()
    {
        _preferences = GetRequiredService<IPreferenceAppService>();
        _principal = GetRequiredService<ICurrentPrincipalAccessor>();
        _products = GetRequiredService<IProductoRepository>();
    }

    private IDisposable AsUser(Guid id) => _principal.Change(new ClaimsPrincipal(
        new ClaimsIdentity(new[] { new Claim(AbpClaimTypes.UserId, id.ToString()) }, "Test")));

    private async Task SetPreferencesAsync(Guid user, bool showAlerts, params string[] items)
    {
        using (AsUser(user))
        {
            await _preferences.UpdateAsync(new UpdateUserPreferenceDto
            {
                ShowAlerts = showAlerts,
                WatchedItems = items.Select(x => (string?)x).ToList()
            });
        }
    }

    private static IExternalProductCatalogClient CatalogWith(string? ingredientes, params string[] alergenos)
    {
        var catalog = Substitute.For<IExternalProductCatalogClient>();
        catalog.GetByBarcodeAsync(Codigo).Returns(new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.Encontrado,
            CodigoBarras = Codigo,
            Nombre = "Producto de prueba",
            Ingredientes = ingredientes,
            Alergenos = alergenos.ToList()
        });
        return catalog;
    }

    private ConsultaProductoExternoAppService ConsultaService(IExternalProductCatalogClient catalog) =>
        new(catalog, Substitute.For<IExternalProductSearchClient>(), GetRequiredService<IFoodWarningEvaluator>());

    private GuardarProductoExternoAppService GuardarService(IExternalProductCatalogClient catalog) =>
        new(_products, catalog, GetRequiredService<IGuidGenerator>(),
            GetRequiredService<IObjectMapper>(), GetRequiredService<IFoodWarningEvaluator>());

    private Task<ExternalProductDto> ConsultarAsync(ConsultaProductoExternoAppService service) =>
        WithUnitOfWorkAsync(() => service.GetAsync(new ConsultarProductoExternoDto { CodigoBarras = Codigo }));

    [Fact]
    public async Task Abrir_el_detalle_aplica_las_preferencias_de_cada_usuario()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SetPreferencesAsync(userA, true, "Maní");
        await SetPreferencesAsync(userB, true, "Lactosa");
        var service = ConsultaService(CatalogWith("Harina, maní tostado", "en:peanuts"));

        using (AsUser(userA))
        {
            var a = (await ConsultarAsync(service)).Advertencias!;
            a.HasPreferences.ShouldBeTrue();
            a.Matches.Count.ShouldBe(1);
            a.Matches[0].Preference.ShouldBe("Maní");
            a.Matches[0].Field.ShouldBe(FoodField.Ingredients);
            a.Unverifiable.ShouldBeEmpty();
        }

        using (AsUser(userB))
        {
            var b = (await ConsultarAsync(service)).Advertencias!;
            b.HasPreferences.ShouldBeTrue();
            b.Matches.ShouldBeEmpty();
            b.Unverifiable.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Un_usuario_sin_preferencias_recibe_que_no_hay_nada_para_comparar()
    {
        var service = ConsultaService(CatalogWith("Maní", "en:peanuts"));

        using (AsUser(Guid.NewGuid()))
        {
            var result = (await ConsultarAsync(service)).Advertencias!;
            result.HasPreferences.ShouldBeFalse();
            result.Matches.ShouldBeEmpty();
            result.ShowAlerts.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Cambiar_las_preferencias_cambia_la_siguiente_evaluacion()
    {
        var user = Guid.NewGuid();
        var service = ConsultaService(CatalogWith("Maní", "en:peanuts"));

        await SetPreferencesAsync(user, true, "Maní");
        using (AsUser(user))
            (await ConsultarAsync(service)).Advertencias!.Matches.Count.ShouldBe(1);

        await SetPreferencesAsync(user, true, "Lactosa");
        using (AsUser(user))
            (await ConsultarAsync(service)).Advertencias!.Matches.ShouldBeEmpty();
    }

    [Fact]
    public async Task Con_las_alertas_apagadas_el_servidor_sigue_informando_los_datos()
    {
        var user = Guid.NewGuid();
        await SetPreferencesAsync(user, false, "Maní");
        var service = ConsultaService(CatalogWith("Maní", "en:peanuts"));

        using (AsUser(user))
        {
            var result = (await ConsultarAsync(service)).Advertencias!;
            // La opción solo cambia la presentación: Angular decide qué ocultar.
            result.ShowAlerts.ShouldBeFalse();
            result.Matches.Count.ShouldBe(1);
        }
    }

    [Fact]
    public async Task Coincidencia_junto_con_datos_incompletos_informa_ambas_cosas()
    {
        var user = Guid.NewGuid();
        await SetPreferencesAsync(user, true, "Peanuts");
        var service = ConsultaService(CatalogWith(null, "en:peanuts"));

        using (AsUser(user))
        {
            var result = (await ConsultarAsync(service)).Advertencias!;
            result.Matches.Count.ShouldBe(1);
            result.Matches[0].Field.ShouldBe(FoodField.Allergens);
            result.Unverifiable.ShouldBe(new[] { FoodField.Ingredients });
        }
    }

    [Fact]
    public async Task Guardar_nuevo_y_existente_usa_las_preferencias_actuales_sin_guardar_nada_personal()
    {
        var user = Guid.NewGuid();
        var catalog = CatalogWith("Maní", "en:peanuts");
        var service = GuardarService(catalog);
        await SetPreferencesAsync(user, true, "Maní");

        Guid firstId;
        using (AsUser(user))
        {
            var first = await WithUnitOfWorkAsync(() => service.SaveAsync(new() { CodigoBarras = Codigo }));
            first.Advertencias!.Matches.Count.ShouldBe(1);
            firstId = first.Producto!.Id;
        }

        // El producto ya existe: se recalcula con la lista nueva y no se consulta de nuevo al proveedor.
        await SetPreferencesAsync(user, true, "Lactosa");
        using (AsUser(user))
        {
            var second = await WithUnitOfWorkAsync(() => service.SaveAsync(new() { CodigoBarras = Codigo }));
            second.Producto!.Id.ShouldBe(firstId);
            second.Advertencias!.Matches.ShouldBeEmpty();
        }

        await catalog.Received(1).GetByBarcodeAsync(Codigo);
        var stored = await WithUnitOfWorkAsync(() => _products.GetListAsync(x => x.CodigoBarras == Codigo));
        var product = stored.ShouldHaveSingleItem();
        product.Ingredientes.ShouldBe("Maní");
        product.Alergenos.ShouldBe("en:peanuts");
    }

    [Fact]
    public async Task Agregar_a_despensa_evalua_por_usuario_y_el_listado_no_incluye_advertencias()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await SetPreferencesAsync(userA, true, "Maní");
        await SetPreferencesAsync(userB, true, "Lactosa");

        var product = await WithUnitOfWorkAsync(() => _products.InsertAsync(
            Producto.DesdeCatalogo(Guid.NewGuid(), Codigo, "Galletas", null, null, null,
                "Harina, maní", "en:peanuts", null, null), autoSave: true));
        var pantry = GetRequiredService<IPantryAppService>();

        using (AsUser(userA))
        {
            var created = await pantry.CreateAsync(new CreatePantryItemDto
            {
                ProductId = product.Id,
                Quantity = 1,
                Unit = "unidad"
            });
            created.Advertencias!.Matches.Count.ShouldBe(1);

            var list = await pantry.GetListAsync(new PagedResultRequestDto { MaxResultCount = 10 });
            list.Items.ShouldHaveSingleItem().Advertencias.ShouldBeNull();
        }

        using (AsUser(userB))
        {
            var created = await pantry.CreateAsync(new CreatePantryItemDto
            {
                ProductId = product.Id,
                Quantity = 1,
                Unit = "unidad"
            });
            created.Advertencias!.Matches.ShouldBeEmpty();

            var list = await pantry.GetListAsync(new PagedResultRequestDto { MaxResultCount = 10 });
            list.Items.ShouldHaveSingleItem().Id.ShouldBe(created.Id);
        }
    }
}