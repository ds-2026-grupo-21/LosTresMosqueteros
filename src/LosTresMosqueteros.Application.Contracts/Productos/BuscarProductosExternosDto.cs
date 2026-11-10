using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LosTresMosqueteros.Productos;

public class BuscarProductosExternosDto : IValidatableObject
{
    [Required]
    [StringLength(200)]
    public string Texto { get; set; } = string.Empty;

    [Range(1, 10000)]
    public int Pagina { get; set; } = 1;

    [Range(1, 50)]
    public int TamanoPagina { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((long)Pagina * TamanoPagina > 10000)
        {
            yield return new ValidationResult(
                "La búsqueda permite consultar hasta los primeros 10000 resultados.",
                new[] { nameof(Pagina), nameof(TamanoPagina) });
        }
    }
}
