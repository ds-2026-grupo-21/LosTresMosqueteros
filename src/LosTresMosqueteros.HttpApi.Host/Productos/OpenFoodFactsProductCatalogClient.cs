using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LosTresMosqueteros.Productos;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private readonly HttpClient _httpClient;

    public OpenFoodFactsProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"product/{Uri.EscapeDataString(barcode)}?fields=code,product_name,brands,image_front_url");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
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

            return new ExternalProductDto
            {
                Resultado = ResultadoConsultaExterna.Encontrado,
                CodigoBarras = producto.Code,
                Nombre = producto.ProductName,
                Marca = producto.Brands,
                ImagenUrl = producto.ImageFrontUrl
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
}
