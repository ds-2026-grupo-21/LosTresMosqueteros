using System.ComponentModel.DataAnnotations;

namespace LosTresMosqueteros.Productos;

public class ConsultarProductoExternoDto
{
    [Required]
    [MinLength(8)]
    [MaxLength(14)]
    public string CodigoBarras { get; set; } = string.Empty;
}