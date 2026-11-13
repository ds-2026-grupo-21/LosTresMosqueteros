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

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsProductCatalogClient> _logger;

    public OpenFoodFactsProductCatalogClient(
        HttpClient httpClient, ILogger<OpenFoodFactsProductCatalogClient>? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? NullLogger<OpenFoodFactsProductCatalogClient>.Instance;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        var reloj = Stopwatch.StartNew();
        var resultado = ResultadoConsultaExterna.ServicioNoDisponible;
        try
        {
            using var response = await _httpClient.GetAsync(
                $"product/{Uri.EscapeDataString(barcode)}?fields=code,product_name,brands,image_front_url,categories,ingredients_text,allergens_tags,nutriscore_grade,nova_group");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                resultado = ResultadoConsultaExterna.NoEncontrado;
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                resultado = ResultadoConsultaExterna.LimiteDeSolicitudesExcedido;
                return new ExternalProductDto
                {
                    Resultado = ResultadoConsultaExterna.LimiteDeSolicitudesExcedido
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return ServicioNoDisponible();
            }

            var contenido = await response.Content.ReadFromJsonAsync<OpenFoodFactsProductResponse>();
            var producto = contenido?.Product;

            if (producto is null)
            {
                return ServicioNoDisponible();
            }

            resultado = ResultadoConsultaExterna.Encontrado;
            return new ExternalProductDto
            {
                Resultado = ResultadoConsultaExterna.Encontrado,
                CodigoBarras = TextoDisponible(producto.Code),
                Nombre = TextoDisponible(producto.ProductName),
                Marca = TextoDisponible(producto.Brands),
                ImagenUrl = TextoDisponible(producto.ImageFrontUrl),
                Categorias = TextoDisponible(producto.Categories),
                Ingredientes = TextoDisponible(producto.IngredientsText),
                Alergenos = producto.AllergensTags?.Any(x => !string.IsNullOrWhiteSpace(x)) == true
                    ? producto.AllergensTags.Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim()).Distinct().ToList()
                    : null,
                NutriScore = NormalizarNutriScore(producto.NutriScoreGrade),
                GrupoNova = producto.NovaGroup is >= 1 and <= 4 ? producto.NovaGroup : null
            };
        }
        catch (HttpRequestException)
        {
            return ServicioNoDisponible();
        }
        catch (OperationCanceledException)
        {
            return ServicioNoDisponible();
        }
        catch (JsonException)
        {
            return ServicioNoDisponible();
        }
        finally
        {
            _logger.LogInformation(
                "Catálogo externo: {Operacion}; resultado: {Resultado}; duración: {DuracionMs} ms",
                "ConsultaPorCodigo", resultado, reloj.ElapsedMilliseconds);
        }
    }

    private static string? TextoDisponible(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizarNutriScore(string? value)
    {
        var grade = value?.Trim().ToLowerInvariant();
        return grade is "a" or "b" or "c" or "d" or "e" ? grade : null;
    }

    private static ExternalProductDto ServicioNoDisponible()
    {
        return new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.ServicioNoDisponible
        };
    }
}

internal sealed class OpenFoodFactsProductResponse
{
    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; set; }
}

internal sealed class OpenFoodFactsProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }

    [JsonPropertyName("categories")]
    public string? Categories { get; set; }

    [JsonPropertyName("ingredients_text")]
    public string? IngredientsText { get; set; }

    [JsonPropertyName("allergens_tags")]
    public List<string>? AllergensTags { get; set; }

    [JsonPropertyName("nutriscore_grade")]
    public string? NutriScoreGrade { get; set; }

    [JsonPropertyName("nova_group")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? NovaGroup { get; set; }
}
