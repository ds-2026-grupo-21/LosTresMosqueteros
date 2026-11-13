using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LosTresMosqueteros.Productos;

public class SearchALiciousProductSearchClient : IExternalProductSearchClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchALiciousProductSearchClient> _logger;

    public SearchALiciousProductSearchClient(
        HttpClient httpClient, ILogger<SearchALiciousProductSearchClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<SearchALiciousProductSearchClient>.Instance;
    }

    public async Task<ExternalProductSearchResultDto> SearchAsync(string text, int page, int pageSize)
    {
        var reloj = Stopwatch.StartNew();
        var result = new ExternalProductSearchResultDto
        {
            Resultado = ResultadoConsultaExterna.ServicioNoDisponible,
            Pagina = page,
            TamanoPagina = pageSize
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("search", new
            {
                q = text,
                page,
                page_size = pageSize,
                langs = new[] { "es", "en" },
                fields = new[] { "code", "product_name", "brands", "image_front_url" }
            });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                result.Resultado = ResultadoConsultaExterna.LimiteDeSolicitudesExcedido;
                return result;
            }

            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            var content = await response.Content.ReadFromJsonAsync<SearchALiciousResponse>();
            if (content?.Hits is null || content.TimedOut != false ||
                content.Page != page || content.PageSize != pageSize ||
                content.Count is null or < 0 || content.PageCount is null or < 0 ||
                content.IsCountExact is null || content.Hits.Count > pageSize ||
                content.Count < content.Hits.Count ||
                content.Hits.Any(x => x is null || string.IsNullOrWhiteSpace(x.Code)))
            {
                return result;
            }

            result.Productos = content.Hits.Select(x => new ExternalProductSummaryDto
            {
                CodigoBarras = x.Code!,
                Nombre = TextoDisponible(x.ProductName),
                Marca = x.Brands?.Any(v => !string.IsNullOrWhiteSpace(v)) == true
                    ? string.Join(", ", x.Brands.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()))
                    : null,
                ImagenUrl = TextoDisponible(x.ImageFrontUrl)
            }).ToList();
            result.TotalResultados = content.Count;
            result.TotalEsExacto = content.IsCountExact;
            result.PaginasDisponibles = Math.Min(content.PageCount.Value, 10000 / pageSize);
            result.Resultado = result.Productos.Count == 0
                ? ResultadoConsultaExterna.NoEncontrado
                : ResultadoConsultaExterna.Encontrado;
            return result;
        }
        catch (HttpRequestException)
        {
            return result;
        }
        catch (OperationCanceledException)
        {
            return result;
        }
        catch (JsonException)
        {
            return result;
        }
        finally
        {
            _logger.LogInformation(
                "Catálogo externo: {Operacion}; resultado: {Resultado}; duración: {DuracionMs} ms",
                "BusquedaPorTexto", result.Resultado, reloj.ElapsedMilliseconds);
        }
    }

    private static string? TextoDisponible(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal sealed class SearchALiciousResponse
{
    [JsonPropertyName("hits")]
    public List<SearchALiciousProduct>? Hits { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    [JsonPropertyName("page_count")]
    public int? PageCount { get; set; }

    [JsonPropertyName("count")]
    public long? Count { get; set; }

    [JsonPropertyName("is_count_exact")]
    public bool? IsCountExact { get; set; }

    [JsonPropertyName("timed_out")]
    public bool? TimedOut { get; set; }
}

internal sealed class SearchALiciousProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("brands")]
    public List<string>? Brands { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }
}
