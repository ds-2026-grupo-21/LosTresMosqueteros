using System;
using Volo.Abp.Application.Dtos;

namespace LosTresMosqueteros.Productos;

public class ProductoDto : FullAuditedEntityDto<Guid>
{
    public string CodigoBarras { get; set; }
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? ImagenUrl { get; set; }
    public string? Categorias { get; set; }
    public string? NutriScore { get; set; }
    public int? GrupoNova { get; set; }
    public string? Ingredientes { get; set; }
    public string? Alergenos { get; set; }
}
