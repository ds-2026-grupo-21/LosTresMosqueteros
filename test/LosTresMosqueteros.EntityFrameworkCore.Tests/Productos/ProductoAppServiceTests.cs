using System;
using System.Threading.Tasks;
using LosTresMosqueteros.Productos;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class ProductoAppServiceTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private readonly IProductoAppService _productoAppService;

    public ProductoAppServiceTests()
    {
        _productoAppService = GetRequiredService<IProductoAppService>();
    }

    [Fact]
    public async Task Deberia_Crear_Y_Luego_Recuperar_Producto_Por_Id()
    {
        var input = new CreateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = "Leche entera 1L",
            Ingredientes = "Leche entera pasteurizada",
            Alergenos = "Contiene lactosa"
        };

        var creado = await _productoAppService.CreateAsync(input);
        var recuperado = await _productoAppService.GetAsync(creado.Id);

        recuperado.Id.ShouldBe(creado.Id);
        recuperado.CodigoBarras.ShouldBe("7791234567890");
        recuperado.Nombre.ShouldBe("Leche entera 1L");
    }

    [Fact]
    public async Task Deberia_Rechazar_Creacion_Sin_Nombre()
    {
        var input = new CreateProductoDto
        {
            CodigoBarras = "7791234567890",
            Nombre = null
        };

        await Should.ThrowAsync<AbpValidationException>(async () =>
        {
            await _productoAppService.CreateAsync(input);
        });
    }
}