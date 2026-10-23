using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros.Productos;

public interface IConsultaProductoExternoAppService : IApplicationService
{
    Task<ExternalProductDto> GetAsync(ConsultarProductoExternoDto input);
}