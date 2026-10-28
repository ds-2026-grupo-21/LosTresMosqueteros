using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros.Productos;

[Authorize]
public class ConsultaProductoExternoAppService : ApplicationService, IConsultaProductoExternoAppService
{
    private readonly IExternalProductCatalogClient _catalogClient;

    public ConsultaProductoExternoAppService(IExternalProductCatalogClient catalogClient)
    {
        _catalogClient = catalogClient;
    }

    public async Task<ExternalProductDto> GetAsync(ConsultarProductoExternoDto input)
    {
        var resultado = await _catalogClient.GetByBarcodeAsync(input.CodigoBarras);

        return resultado ?? new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.NoEncontrado
        };
    }
}