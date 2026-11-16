using System.Threading.Tasks;
using LosTresMosqueteros.Preferences;
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

    /// <summary>Evaluación personal del usuario actual. Se calcula al momento; no se guarda en el producto.</summary>
    public FoodWarningDto? Advertencias { get; set; }
}