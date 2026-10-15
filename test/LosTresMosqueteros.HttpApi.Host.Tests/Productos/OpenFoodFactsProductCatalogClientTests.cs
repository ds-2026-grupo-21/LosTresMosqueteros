using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class OpenFoodFactsProductCatalogClientTests
{
    private const string CodigoBarras = "3017620422003";

    [Fact]
    public async Task Deberia_Consultar_La_Ruta_V3_Y_Mapear_El_Producto()
    {
        var handler = new RespuestaSimuladaHandler(() => RespuestaJson("""
            {
                "product": {
                    "code": "3017620422003",
                    "product_name": "Nutella",
                    "brands": "Ferrero",
                    "image_front_url": "https://example.com/nutella.jpg"
                }
            }
            """));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        handler.Metodo.ShouldBe(HttpMethod.Get);
        handler.UrlConsultada.ShouldNotBeNull();
        handler.UrlConsultada.AbsoluteUri.ShouldBe(
            "https://world.openfoodfacts.org/api/v3/product/3017620422003?fields=code,product_name,brands,image_front_url");
        handler.Solicitudes.ShouldBe(1);
        resultado.ShouldNotBeNull();
        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        resultado.CodigoBarras.ShouldBe(CodigoBarras);
        resultado.Nombre.ShouldBe("Nutella");
        resultado.Marca.ShouldBe("Ferrero");
        resultado.ImagenUrl.ShouldBe("https://example.com/nutella.jpg");
    }

    [Fact]
    public async Task Deberia_Conservar_Null_En_Los_Campos_Ausentes_Del_Json()
    {
        var handler = new RespuestaSimuladaHandler(() => RespuestaJson("""
            { "product": { "code": "3017620422003" } }
            """));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        resultado.ShouldNotBeNull();
        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        resultado.CodigoBarras.ShouldBe(CodigoBarras);
        resultado.Nombre.ShouldBeNull();
        resultado.Marca.ShouldBeNull();
        resultado.ImagenUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Deberia_Devolver_Null_Cuando_El_Proveedor_Responde_404()
    {
        var handler = new RespuestaSimuladaHandler(() => RespuestaError(HttpStatusCode.NotFound));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        resultado.ShouldBeNull();
    }

    [Fact]
    public async Task Deberia_Devolver_LimiteDeSolicitudesExcedido_Cuando_El_Proveedor_Responde_429()
    {
        var handler = new RespuestaSimuladaHandler(() => RespuestaError(HttpStatusCode.TooManyRequests));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        resultado.ShouldNotBeNull();
        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.LimiteDeSolicitudesExcedido);
        resultado.CodigoBarras.ShouldBeNull();
        resultado.Nombre.ShouldBeNull();
        resultado.Marca.ShouldBeNull();
        resultado.ImagenUrl.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Deberia_Devolver_ServicioNoDisponible_Cuando_El_Proveedor_Responde_Error(
        HttpStatusCode statusCode)
    {
        var handler = new RespuestaSimuladaHandler(() => RespuestaError(statusCode));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        VerificarServicioNoDisponible(resultado);
    }

    [Fact]
    public async Task Deberia_Devolver_ServicioNoDisponible_Si_Falla_La_Conexion()
    {
        var handler = new RespuestaSimuladaHandler(() => throw new HttpRequestException("Fallo de conexión simulado"));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        VerificarServicioNoDisponible(resultado);
    }

    [Fact]
    public async Task Deberia_Devolver_ServicioNoDisponible_Si_La_Solicitud_Se_Cancela()
    {
        var handler = new RespuestaSimuladaHandler(() => throw new TaskCanceledException("Timeout simulado"));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        VerificarServicioNoDisponible(resultado);
    }

    [Fact]
    public async Task Deberia_Devolver_ServicioNoDisponible_Si_El_Json_Es_Invalido()
    {
        var handler = new RespuestaSimuladaHandler(() => RespuestaJson("{ JSON inválido"));
        using var httpClient = CrearClienteHttp(handler);
        var catalogClient = new OpenFoodFactsProductCatalogClient(httpClient);

        var resultado = await catalogClient.GetByBarcodeAsync(CodigoBarras);

        VerificarServicioNoDisponible(resultado);
    }

    private static HttpClient CrearClienteHttp(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/api/v3/")
        };
    }

    private static HttpResponseMessage RespuestaJson(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage RespuestaError(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("Error del proveedor simulado")
        };
    }

    private static void VerificarServicioNoDisponible(ExternalProductDto? resultado)
    {
        resultado.ShouldNotBeNull();
        resultado.Resultado.ShouldBe(ResultadoConsultaExterna.ServicioNoDisponible);
        resultado.CodigoBarras.ShouldBeNull();
        resultado.Nombre.ShouldBeNull();
        resultado.Marca.ShouldBeNull();
        resultado.ImagenUrl.ShouldBeNull();
    }

    private sealed class RespuestaSimuladaHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respuesta;

        public HttpMethod? Metodo { get; private set; }
        public Uri? UrlConsultada { get; private set; }
        public int Solicitudes { get; private set; }

        public RespuestaSimuladaHandler(Func<HttpResponseMessage> respuesta)
        {
            _respuesta = respuesta;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Metodo = request.Method;
            UrlConsultada = request.RequestUri;
            Solicitudes++;
            return Task.FromResult(_respuesta());
        }
    }
}
