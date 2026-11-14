using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using LosTresMosqueteros.Preferences;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Validation;

namespace LosTresMosqueteros.Productos;

[Authorize]
public class ConsultaProductoExternoAppService : ApplicationService, IConsultaProductoExternoAppService
{
    private readonly IExternalProductCatalogClient _catalogClient;
    private readonly IExternalProductSearchClient _searchClient;
    private readonly IFoodWarningEvaluator _warnings;

    public ConsultaProductoExternoAppService(
        IExternalProductCatalogClient catalogClient,
        IExternalProductSearchClient searchClient,
        IFoodWarningEvaluator warnings)
    {
        _catalogClient = catalogClient;
        _searchClient = searchClient;
        _warnings = warnings;
    }

    public async Task<ExternalProductDto> GetAsync(ConsultarProductoExternoDto input)
    {
        ValidarEntrada(input);
        var resultado = await _catalogClient.GetByBarcodeAsync(input.CodigoBarras);

        if (resultado == null)
        {
            return new ExternalProductDto { Resultado = ResultadoConsultaExterna.NoEncontrado };
        }

        if (resultado.Resultado != ResultadoConsultaExterna.Encontrado)
        {
            return resultado;
        }

        // Se devuelve una copia: el resultado personal no se escribe en el objeto del proveedor.
        var alergenos = resultado.Alergenos is { Count: > 0 }
            ? string.Join(", ", resultado.Alergenos)
            : null;

        return new ExternalProductDto
        {
            Resultado = resultado.Resultado,
            CodigoBarras = resultado.CodigoBarras,
            Nombre = resultado.Nombre,
            Marca = resultado.Marca,
            ImagenUrl = resultado.ImagenUrl,
            Categorias = resultado.Categorias,
            Ingredientes = resultado.Ingredientes,
            Alergenos = resultado.Alergenos,
            NutriScore = resultado.NutriScore,
            GrupoNova = resultado.GrupoNova,
            Advertencias = await _warnings.EvaluateAsync(resultado.Ingredientes, alergenos)
        };
    }

    public Task<ExternalProductSearchResultDto> SearchAsync(BuscarProductosExternosDto input)
    {
        ValidarEntrada(input);
        return _searchClient.SearchAsync(input.Texto.Trim(), input.Pagina, input.TamanoPagina);
    }

    private static void ValidarEntrada(object input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), errores, true))
        {
            throw new AbpValidationException("La consulta no es válida.", errores);
        }
    }
}