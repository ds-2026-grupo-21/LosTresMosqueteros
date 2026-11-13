using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros.Productos;

public interface IGuardarProductoExternoAppService : IApplicationService
{
    Task<GuardarProductoExternoResultadoDto> SaveAsync(ConsultarProductoExternoDto input);
}

public class GuardarProductoExternoResultadoDto
{
    public ResultadoConsultaExterna Resultado { get; set; }
    public ProductoDto? Producto { get; set; }
}
