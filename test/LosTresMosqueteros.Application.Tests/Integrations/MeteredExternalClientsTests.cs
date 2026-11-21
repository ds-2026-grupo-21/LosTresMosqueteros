using System;
using System.Threading.Tasks;
using LosTresMosqueteros.Productos;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Integrations;

public class MeteredExternalClientsTests
{
    private readonly IExternalApiCallRecorder _recorder = Substitute.For<IExternalApiCallRecorder>();

    [Theory]
    [InlineData(ResultadoConsultaExterna.Encontrado, ExternalApiOutcome.Found)]
    [InlineData(ResultadoConsultaExterna.NoEncontrado, ExternalApiOutcome.NotFound)]
    [InlineData(ResultadoConsultaExterna.LimiteDeSolicitudesExcedido, ExternalApiOutcome.RateLimited)]
    [InlineData(ResultadoConsultaExterna.ServicioNoDisponible, ExternalApiOutcome.Unavailable)]
    public async Task La_consulta_por_codigo_registra_el_resultado_y_devuelve_la_misma_respuesta(
        ResultadoConsultaExterna resultado, ExternalApiOutcome expected)
    {
        var response = new ExternalProductDto { Resultado = resultado, Nombre = "Leche" };
        var inner = Substitute.For<IExternalProductCatalogClient>();
        inner.GetByBarcodeAsync("0779123").Returns(response);

        var result = await new MeteredExternalProductCatalogClient(inner, _recorder).GetByBarcodeAsync("0779123");

        result.ShouldBeSameAs(response);
        await inner.Received(1).GetByBarcodeAsync("0779123");
        await _recorder.Received(1).RecordAsync(ExternalApiOperation.BarcodeLookup, expected, Arg.Is<long>(x => x >= 0));
    }

    [Fact]
    public async Task Un_producto_inexistente_se_registra_como_no_encontrado()
    {
        var inner = Substitute.For<IExternalProductCatalogClient>();
        inner.GetByBarcodeAsync(Arg.Any<string>()).Returns((ExternalProductDto?)null);

        var result = await new MeteredExternalProductCatalogClient(inner, _recorder).GetByBarcodeAsync("123");

        result.ShouldBeNull();
        await _recorder.Received(1).RecordAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.NotFound, Arg.Any<long>());
    }

    [Fact]
    public async Task Un_error_inesperado_se_registra_y_se_propaga()
    {
        var inner = Substitute.For<IExternalProductCatalogClient>();
        inner.GetByBarcodeAsync(Arg.Any<string>())
            .Returns(Task.FromException<ExternalProductDto?>(new InvalidOperationException("falla")));

        await Should.ThrowAsync<InvalidOperationException>(
            () => new MeteredExternalProductCatalogClient(inner, _recorder).GetByBarcodeAsync("123"));

        await _recorder.Received(1).RecordAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Error, Arg.Any<long>());
    }

    [Theory]
    [InlineData(ResultadoConsultaExterna.Encontrado, ExternalApiOutcome.Found)]
    [InlineData(ResultadoConsultaExterna.NoEncontrado, ExternalApiOutcome.NotFound)]
    [InlineData(ResultadoConsultaExterna.LimiteDeSolicitudesExcedido, ExternalApiOutcome.RateLimited)]
    [InlineData(ResultadoConsultaExterna.ServicioNoDisponible, ExternalApiOutcome.Unavailable)]
    public async Task La_busqueda_por_texto_registra_el_resultado_y_devuelve_la_misma_respuesta(
        ResultadoConsultaExterna resultado, ExternalApiOutcome expected)
    {
        var response = new ExternalProductSearchResultDto { Resultado = resultado, Pagina = 2, TamanoPagina = 10 };
        var inner = Substitute.For<IExternalProductSearchClient>();
        inner.SearchAsync("dulce de leche", 2, 10).Returns(response);

        var result = await new MeteredExternalProductSearchClient(inner, _recorder).SearchAsync("dulce de leche", 2, 10);

        result.ShouldBeSameAs(response);
        await inner.Received(1).SearchAsync("dulce de leche", 2, 10);
        await _recorder.Received(1).RecordAsync(ExternalApiOperation.TextSearch, expected, Arg.Is<long>(x => x >= 0));
    }

    [Fact]
    public async Task Un_error_inesperado_en_la_busqueda_se_registra_y_se_propaga()
    {
        var inner = Substitute.For<IExternalProductSearchClient>();
        inner.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Task.FromException<ExternalProductSearchResultDto>(new InvalidOperationException("falla")));

        await Should.ThrowAsync<InvalidOperationException>(
            () => new MeteredExternalProductSearchClient(inner, _recorder).SearchAsync("arroz", 1, 10));

        await _recorder.Received(1).RecordAsync(ExternalApiOperation.TextSearch, ExternalApiOutcome.Error, Arg.Any<long>());
    }
}
