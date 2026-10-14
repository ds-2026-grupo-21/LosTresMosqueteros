using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class ConsultaProductoExternoAppServiceTests
{
    private const string CodigoBarras = "7791234567890";
    private readonly IExternalProductCatalogClient _catalogClient;
    private readonly ConsultaProductoExternoAppService _appService;

    public ConsultaProductoExternoAppServiceTests()
    {
        _catalogClient = Substitute.For<IExternalProductCatalogClient>();
        _appService = new ConsultaProductoExternoAppService(_catalogClient);
    }

    [Fact]
    public async Task Deberia_Devolver_El_Producto_Encontrado()
    {
        var producto = new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.Encontrado,
            CodigoBarras = CodigoBarras,
            Nombre = "Leche entera 1L",
            Marca = "Marca de prueba",
            ImagenUrl = "https://example.com/producto.jpg"
        };

        var resultado = await ConsultarAsync(producto);

        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        resultado.CodigoBarras.ShouldBe(CodigoBarras);
        resultado.Nombre.ShouldBe("Leche entera 1L");
        resultado.Marca.ShouldBe("Marca de prueba");
        resultado.ImagenUrl.ShouldBe("https://example.com/producto.jpg");
    }

    [Fact]
    public async Task Deberia_Devolver_NoEncontrado_Cuando_El_Cliente_Devuelve_Null()
    {
        var resultado = await ConsultarAsync(null);

        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.NoEncontrado);
        resultado.CodigoBarras.ShouldBeNull();
        resultado.Nombre.ShouldBeNull();
        resultado.Marca.ShouldBeNull();
        resultado.ImagenUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Deberia_Conservar_Null_En_Los_Datos_Parciales_Ausentes()
    {
        var producto = new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.Encontrado,
            CodigoBarras = CodigoBarras
        };

        var resultado = await ConsultarAsync(producto);

        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        resultado.CodigoBarras.ShouldBe(CodigoBarras);
        resultado.Nombre.ShouldBeNull();
        resultado.Marca.ShouldBeNull();
        resultado.ImagenUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Deberia_Conservar_El_Limite_De_Solicitudes_Excedido()
    {
        var respuesta = new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.LimiteDeSolicitudesExcedido
        };

        var resultado = await ConsultarAsync(respuesta);

        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.LimiteDeSolicitudesExcedido);
        resultado.CodigoBarras.ShouldBeNull();
        resultado.Nombre.ShouldBeNull();
        resultado.Marca.ShouldBeNull();
        resultado.ImagenUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Deberia_Conservar_El_Servicio_No_Disponible()
    {
        var respuesta = new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.ServicioNoDisponible
        };

        var resultado = await ConsultarAsync(respuesta);

        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.ServicioNoDisponible);
        resultado.CodigoBarras.ShouldBeNull();
        resultado.Nombre.ShouldBeNull();
        resultado.Marca.ShouldBeNull();
        resultado.ImagenUrl.ShouldBeNull();
    }

    private async Task<ExternalProductDto> ConsultarAsync(ExternalProductDto? respuesta)
    {
        _catalogClient.GetByBarcodeAsync(CodigoBarras)
            .Returns(Task.FromResult(respuesta));

        var resultado = await _appService.GetAsync(new ConsultarProductoExternoDto
        {
            CodigoBarras = CodigoBarras
        });

        await _catalogClient.Received(1).GetByBarcodeAsync(Arg.Any<string>());
        await _catalogClient.Received(1).GetByBarcodeAsync(CodigoBarras);

        resultado.ShouldNotBeNull();
        return resultado;
    }
}
