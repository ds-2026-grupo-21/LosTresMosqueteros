using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LosTresMosqueteros.Preferences;

public class UpdateUserPreferenceDto : IValidatableObject
{
    public bool ShowAlerts { get; set; }
    public string? PhotoDataUrl { get; set; }

    public List<string?> WatchedItems { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PreferencePhoto.IsValid(PhotoDataUrl))
            yield return new ValidationResult("La foto debe ser PNG o JPEG de hasta 80 kB.", new[] { nameof(PhotoDataUrl) });
        if (WatchedItems == null)
        {
            yield return new ValidationResult(
                "La lista de preferencias es obligatoria.",
                new[] { nameof(WatchedItems) });
            yield break;
        }

        if (WatchedItems.Count > PreferenceConsts.MaxWatchedItems)
        {
            yield return new ValidationResult(
                $"Se admiten hasta {PreferenceConsts.MaxWatchedItems} entradas.",
                new[] { nameof(WatchedItems) });
        }

        foreach (var item in WatchedItems)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                yield return new ValidationResult(
                    "No se admiten entradas vacías.",
                    new[] { nameof(WatchedItems) });
            }
            else if (item.Trim().Length > PreferenceConsts.MaxNameLength)
            {
                yield return new ValidationResult(
                    $"Cada entrada puede tener hasta {PreferenceConsts.MaxNameLength} caracteres.",
                    new[] { nameof(WatchedItems) });
            }
        }
    }
}
