using System.ComponentModel.DataAnnotations;
using LosTresMosqueteros.Productos;

namespace LosTresMosqueteros.Productos;

public class CreateProductoDto
{
    [Required]
    [StringLength(ProductoConsts.MaxCodigoBarrasLength)]
    public string CodigoBarras { get; set; }

    [Required]
    [StringLength(ProductoConsts.MaxNombreLength)]
    public string Nombre { get; set; }

    [StringLength(ProductoConsts.MaxIngredientesLength)]
    public string? Ingredientes { get; set; }

    [StringLength(ProductoConsts.MaxAlergenosLength)]
    public string? Alergenos { get; set; }
}