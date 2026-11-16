using System.Threading.Tasks;
using LosTresMosqueteros.Preferences;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class ConsultaProductoExternoAppServiceTests
{
    private const string CodigoBarras = "7791234567890";
    private readonly IExternalProductCatalogClient _catalogClient;
    private readonly IFoodWarningEvaluator _warnings;
    private readonly ConsultaProductoExternoAppService _appService;

    public ConsultaProductoExternoAppServiceTests()
    {
        _catalogClient = Substitute.For<IExternalProductCatalogClient>();
        _warnings = Substitute.For<IFoodWarningEvaluator>();
        _warnings.EvaluateAsync(Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(Task.FromResult(new FoodWarningDto { HasPreferences = false }));

        _appService = new ConsultaProductoExternoAppService(
            _catalogClient, Substitute.For<IExternalProductSearchClient>(), _warnings);
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
            ImagenUrl = "https://example.com/producto.jpg",
            Categorias = "Lácteos",
            Ingredientes = "Leche",
            Alergenos = new() { "en:milk" },
            NutriScore = "b",
            GrupoNova = 1
        };

        var resultado = await ConsultarAsync(producto);

        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        resultado.CodigoBarras.ShouldBe(CodigoBarras);
        resultado.Nombre.ShouldBe("Leche entera 1L");
        resultado.Marca.ShouldBe("Marca de prueba");
        resultado.ImagenUrl.ShouldBe("https://example.com/producto.jpg");
        resultado.Categorias.ShouldBe("Lácteos");
        resultado.Ingredientes.ShouldBe("Leche");
        resultado.Alergenos.ShouldBe(new[] { "en:milk" });
        resultado.NutriScore.ShouldBe("b");
        resultado.GrupoNova.ShouldBe(1);
    }

    [Fact]
    public async Task Deberia_Adjuntar_Las_Advertencias_Con_Los_Datos_Ya_Obtenidos()
    {
        var advertencias = new FoodWarningDto { HasPreferences = true };
        _warnings.EvaluateAsync("Leche", "en:milk").Returns(Task.FromResult(advertencias));

        var producto = new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.Encontrado,
            CodigoBarras = CodigoBarras,
            Ingredientes = "Leche",
            Alergenos = new() { "en:milk" }
        };

        var resultado = await ConsultarAsync(producto);

        resultado.Advertencias.ShouldBeSameAs(advertencias);
        await _warnings.Received(1).EvaluateAsync("Leche", "en:milk");
        // El objeto del proveedor no se modifica con datos personales.
        producto.Advertencias.ShouldBeNull();
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
        resultado.Advertencias.ShouldBeNull();
        await _warnings.DidNotReceiveWithAnyArgs().EvaluateAsync(default, default);
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
        await _warnings.Received(1).EvaluateAsync(null, null);
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
        await _warnings.DidNotReceiveWithAnyArgs().EvaluateAsync(default, default);
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
        await _warnings.DidNotReceiveWithAnyArgs().EvaluateAsync(default, default);
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