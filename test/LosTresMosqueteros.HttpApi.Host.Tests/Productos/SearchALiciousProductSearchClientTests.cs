using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class SearchALiciousProductSearchClientTests
{
    private const string RespuestaValida = """
        {"hits":[{"code":"0009800800049","product_name":" Producto de prueba ",
        "brands":["Marca A","Marca B"],"image_front_url":"https://example.com/producto.jpg"}],
        "page":1,"page_size":10,"page_count":2,"count":11,"is_count_exact":true,"timed_out":false}
        """;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Deberia_Buscar_Por_Post_Y_Mapear_Resumen_Y_Paginacion(int pagina)
    {
        var json = JsonNode.Parse(RespuestaValida)!;
        json["page"] = pagina;
        var handler = new RespuestaSimuladaHandler(() => Respuesta(json.ToJsonString()));
        using var http = CrearHttp(handler);
        var client = new SearchALiciousProductSearchClient(http);

        var result = await client.SearchAsync("leche", pagina, 10);

        handler.Metodo.ShouldBe(HttpMethod.Post);
        handler.Url.ShouldBe(new Uri("https://search.openfoodfacts.org/search"));
        handler.Solicitudes.ShouldBe(1);
        using var body = JsonDocument.Parse(handler.Cuerpo!);
        body.RootElement.GetProperty("q").GetString().ShouldBe("leche");
        body.RootElement.GetProperty("page").GetInt32().ShouldBe(pagina);
        body.RootElement.GetProperty("page_size").GetInt32().ShouldBe(10);
        body.RootElement.GetProperty("langs").Deserialize<string[]>().ShouldBe(new[] { "es", "en" });
        body.RootElement.GetProperty("fields").Deserialize<string[]>().ShouldBe(
            new[] { "code", "product_name", "brands", "image_front_url" });
        result.Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        result.Pagina.ShouldBe(pagina);
        result.TamanoPagina.ShouldBe(10);
        result.TotalResultados.ShouldBe(11);
        result.TotalEsExacto.ShouldBe(true);
        result.PaginasDisponibles.ShouldBe(2);
        result.Productos.Count.ShouldBe(1);
        var product = result.Productos[0];
        product.CodigoBarras.ShouldBe("0009800800049");
        product.Nombre.ShouldBe("Producto de prueba");
        product.Marca.ShouldBe("Marca A, Marca B");
        product.ImagenUrl.ShouldBe("https://example.com/producto.jpg");
    }

    [Fact]
    public async Task Deberia_Conservar_Los_Datos_Ausentes_Del_Resumen()
    {
        var json = JsonNode.Parse(RespuestaValida)!;
        json["hits"] = JsonNode.Parse("""[{"code":"0009800800049","product_name":" ","brands":[]}]""");
        using var http = CrearHttp(new RespuestaSimuladaHandler(() => Respuesta(json.ToJsonString())));

        var result = await new SearchALiciousProductSearchClient(http).SearchAsync("arroz", 1, 10);

        result.Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        result.Productos[0].CodigoBarras.ShouldBe("0009800800049");
        result.Productos[0].Nombre.ShouldBeNull();
        result.Productos[0].Marca.ShouldBeNull();
        result.Productos[0].ImagenUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Deberia_Distinguir_Una_Busqueda_Vacia_De_Una_Falla()
    {
        using var http = CrearHttp(new RespuestaSimuladaHandler(() => Respuesta("""
            {"hits":[],"page":1,"page_size":10,"page_count":0,"count":0,
             "is_count_exact":true,"timed_out":false}
            """)));

        var result = await new SearchALiciousProductSearchClient(http).SearchAsync("sin resultados", 1, 10);

        result.Resultado.ShouldBe(ResultadoConsultaExterna.NoEncontrado);
        result.Productos.ShouldBeEmpty();
        result.TotalResultados.ShouldBe(0);
        result.TotalEsExacto.ShouldBe(true);
        result.PaginasDisponibles.ShouldBe(0);
    }

    [Fact]
    public async Task Deberia_Informar_Un_Total_Aproximado_Y_Limitar_Las_Paginas_Accesibles()
    {
        var json = JsonNode.Parse(RespuestaValida)!;
        json["count"] = 10000;
        json["is_count_exact"] = false;
        json["page_size"] = 30;
        json["page_count"] = 334;
        using var http = CrearHttp(new RespuestaSimuladaHandler(() => Respuesta(json.ToJsonString())));

        var result = await new SearchALiciousProductSearchClient(http).SearchAsync("leche", 1, 30);

        result.TotalResultados.ShouldBe(10000);
        result.TotalEsExacto.ShouldBe(false);
        result.PaginasDisponibles.ShouldBe(333);
    }

    [Theory]
    [InlineData(429, ResultadoConsultaExterna.LimiteDeSolicitudesExcedido)]
    [InlineData(400, ResultadoConsultaExterna.ServicioNoDisponible)]
    [InlineData(404, ResultadoConsultaExterna.ServicioNoDisponible)]
    [InlineData(422, ResultadoConsultaExterna.ServicioNoDisponible)]
    [InlineData(500, ResultadoConsultaExterna.ServicioNoDisponible)]
    [InlineData(503, ResultadoConsultaExterna.ServicioNoDisponible)]
    public async Task Deberia_Controlar_Errores_Http_Sin_Publicar_El_Cuerpo(int status,
        ResultadoConsultaExterna expected)
    {
        using var http = CrearHttp(new RespuestaSimuladaHandler(() => Respuesta(
            "contenido privado del proveedor", (HttpStatusCode)status)));
        var logger = new RegistroDePrueba<SearchALiciousProductSearchClient>();

        var result = await new SearchALiciousProductSearchClient(http, logger)
            .SearchAsync("consulta que no debe registrarse", 2, 10);

        result.Resultado.ShouldBe(expected);
        result.Pagina.ShouldBe(2);
        result.TamanoPagina.ShouldBe(10);
        result.Productos.ShouldBeEmpty();
        result.TotalResultados.ShouldBeNull();
        result.TotalEsExacto.ShouldBeNull();
        result.PaginasDisponibles.ShouldBeNull();
        VerificarRegistro(logger, expected);
    }

    [Theory]
    [InlineData("{json inválido")]
    [InlineData("null")]
    [InlineData("{\"errors\":[\"error del proveedor\"]}")]
    public async Task Deberia_Controlar_Respuestas_Invalidas(string json)
    {
        using var http = CrearHttp(new RespuestaSimuladaHandler(() => Respuesta(json)));

        var result = await new SearchALiciousProductSearchClient(http).SearchAsync("leche", 1, 10);

        result.Resultado.ShouldBe(ResultadoConsultaExterna.ServicioNoDisponible);
        result.TotalResultados.ShouldBeNull();
        result.Productos.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("timed_out", "true")]
    [InlineData("count", "null")]
    [InlineData("is_count_exact", "null")]
    [InlineData("page_count", "-1")]
    [InlineData("page", "2")]
    [InlineData("page_size", "50")]
    [InlineData("hits", "[{\"product_name\":\"Sin identificador\"}]")]
    [InlineData("hits", "[null]")]
    public async Task Deberia_Rechazar_Datos_Que_No_Permiten_Navegar_La_Busqueda(string field, string value)
    {
        var json = JsonNode.Parse(RespuestaValida)!;
        json[field] = JsonNode.Parse(value);
        using var http = CrearHttp(new RespuestaSimuladaHandler(() => Respuesta(json.ToJsonString())));

        var result = await new SearchALiciousProductSearchClient(http).SearchAsync("leche", 1, 10);

        result.Resultado.ShouldBe(ResultadoConsultaExterna.ServicioNoDisponible);
        result.Productos.ShouldBeEmpty();
        result.TotalResultados.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deberia_Controlar_Conexion_Y_Timeout_Sin_Registrar_Datos_De_La_Excepcion(bool timeout)
    {
        using var http = CrearHttp(new RespuestaSimuladaHandler(() =>
        {
            if (timeout) throw new TaskCanceledException("detalle privado");
            throw new HttpRequestException("detalle privado");
        }));
        var logger = new RegistroDePrueba<SearchALiciousProductSearchClient>();

        var result = await new SearchALiciousProductSearchClient(http, logger).SearchAsync("leche", 1, 10);

        result.Resultado.ShouldBe(ResultadoConsultaExterna.ServicioNoDisponible);
        VerificarRegistro(logger, result.Resultado);
    }

    private static void VerificarRegistro(RegistroDePrueba<SearchALiciousProductSearchClient> logger,
        ResultadoConsultaExterna expected)
    {
        logger.Entradas.Count.ShouldBe(1);
        var entry = logger.Entradas[0];
        entry.Count.ShouldBe(4);
        entry["Operacion"].ShouldBe("BusquedaPorTexto");
        entry["Resultado"].ShouldBe(expected);
        ((long)entry["DuracionMs"]!).ShouldBeGreaterThanOrEqualTo(0);
        entry.ContainsKey("{OriginalFormat}").ShouldBeTrue();
        logger.Excepciones[0].ShouldBeNull();
    }

    private static HttpClient CrearHttp(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://search.openfoodfacts.org/")
    };

    private static HttpResponseMessage Respuesta(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class RespuestaSimuladaHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
    {
        public HttpMethod? Metodo { get; private set; }
        public Uri? Url { get; private set; }
        public string? Cuerpo { get; private set; }
        public int Solicitudes { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Metodo = request.Method;
            Url = request.RequestUri;
            Cuerpo = await request.Content!.ReadAsStringAsync(cancellationToken);
            Solicitudes++;
            return response();
        }
    }
}
