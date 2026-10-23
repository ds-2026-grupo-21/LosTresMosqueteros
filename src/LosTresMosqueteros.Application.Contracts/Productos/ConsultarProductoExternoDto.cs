using System.ComponentModel.DataAnnotations;

namespace LosTresMosqueteros.Productos;

public class ConsultarProductoExternoDto
{
    [Required]
    [MinLength(8)]
    [MaxLength(14)]
    [RegularExpression(@"^[0-9]+$")]
    public string CodigoBarras { get; set; } = string.Empty;
}
