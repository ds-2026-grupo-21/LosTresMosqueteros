using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Validation;

namespace LosTresMosqueteros.Productos;

[Authorize]
public class ConsultaProductoExternoAppService : ApplicationService, IConsultaProductoExternoAppService
{
    private readonly IExternalProductCatalogClient _catalogClient;
    private readonly IExternalProductSearchClient _searchClient;

    public ConsultaProductoExternoAppService(
        IExternalProductCatalogClient catalogClient,
        IExternalProductSearchClient searchClient)
    {
        _catalogClient = catalogClient;
        _searchClient = searchClient;
    }

    public async Task<ExternalProductDto> GetAsync(ConsultarProductoExternoDto input)
    {
        ValidarEntrada(input);
        var resultado = await _catalogClient.GetByBarcodeAsync(input.CodigoBarras);

        return resultado ?? new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.NoEncontrado
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
