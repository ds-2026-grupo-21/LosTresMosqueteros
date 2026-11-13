using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class BusquedaProductosExternosAppServiceTests
{
    private readonly IExternalProductCatalogClient _catalog = Substitute.For<IExternalProductCatalogClient>();
    private readonly IExternalProductSearchClient _search = Substitute.For<IExternalProductSearchClient>();
    private readonly ConsultaProductoExternoAppService _service;

    public BusquedaProductosExternosAppServiceTests()
    {
        _service = new ConsultaProductoExternoAppService(_catalog, _search);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Deberia_Enviar_Texto_Y_Paginacion_Al_Cliente(int pagina)
    {
        var expected = new ExternalProductSearchResultDto
        {
            Resultado = ResultadoConsultaExterna.Encontrado,
            Pagina = pagina,
            TamanoPagina = 2,
            TotalResultados = 3,
            TotalEsExacto = true,
            PaginasDisponibles = 2,
            Productos = new() { new() { CodigoBarras = "0009800800049", Nombre = "Producto de prueba" } }
        };
        _search.SearchAsync("leche", pagina, 2).Returns(expected);

        var actual = await _service.SearchAsync(new BuscarProductosExternosDto
        {
            Texto = "  leche  ", Pagina = pagina, TamanoPagina = 2
        });

        actual.ShouldBeSameAs(expected);
        await _search.Received(1).SearchAsync("leche", pagina, 2);
        await _catalog.DidNotReceiveWithAnyArgs().GetByBarcodeAsync(default!);
    }

    [Theory]
    [InlineData(ResultadoConsultaExterna.NoEncontrado)]
    [InlineData(ResultadoConsultaExterna.LimiteDeSolicitudesExcedido)]
    [InlineData(ResultadoConsultaExterna.ServicioNoDisponible)]
    public async Task Deberia_Conservar_El_Resultado_De_La_Busqueda(ResultadoConsultaExterna estado)
    {
        var expected = new ExternalProductSearchResultDto
        {
            Resultado = estado, Pagina = 1, TamanoPagina = 10
        };
        _search.SearchAsync("arroz", 1, 10).Returns(expected);

        var actual = await _service.SearchAsync(new BuscarProductosExternosDto { Texto = "arroz" });

        actual.ShouldBeSameAs(expected);
        await _search.Received(1).SearchAsync("arroz", 1, 10);
    }

    [Theory]
    [InlineData("", 1, 10)]
    [InlineData("   ", 1, 10)]
    [InlineData(null, 1, 10)]
    [InlineData("leche", 0, 10)]
    [InlineData("leche", 1, 0)]
    [InlineData("leche", 1, 51)]
    [InlineData("leche", 334, 30)]
    [InlineData("leche", int.MaxValue, 50)]
    public async Task Deberia_Rechazar_Entradas_Invalidas_Sin_Consultar_El_Proveedor(
        string? texto, int pagina, int tamano)
    {
        await Should.ThrowAsync<AbpValidationException>(() => _service.SearchAsync(
            new BuscarProductosExternosDto { Texto = texto!, Pagina = pagina, TamanoPagina = tamano }));

        await _search.DidNotReceiveWithAnyArgs().SearchAsync(default!, default, default);
    }

    [Fact]
    public async Task Deberia_Rechazar_Un_Texto_Demasiado_Largo()
    {
        await Should.ThrowAsync<AbpValidationException>(() => _service.SearchAsync(
            new BuscarProductosExternosDto { Texto = new string('a', 201) }));

        await _search.DidNotReceiveWithAnyArgs().SearchAsync(default!, default, default);
    }

    [Fact]
    public async Task Deberia_Aceptar_El_Limite_De_Resultados()
    {
        _search.SearchAsync("arroz", 200, 50).Returns(new ExternalProductSearchResultDto());

        await _service.SearchAsync(new BuscarProductosExternosDto
        {
            Texto = "arroz", Pagina = 200, TamanoPagina = 50
        });

        await _search.Received(1).SearchAsync("arroz", 200, 50);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("30176204220A3")]
    [InlineData("30176204 2003")]
    public async Task Deberia_Validar_El_Codigo_Antes_De_Consultar(string codigo)
    {
        await Should.ThrowAsync<AbpValidationException>(() => _service.GetAsync(
            new ConsultarProductoExternoDto { CodigoBarras = codigo }));

        await _catalog.DidNotReceiveWithAnyArgs().GetByBarcodeAsync(default!);
    }
}
