using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Volo.Abp.Domain.Entities;

namespace LosTresMosqueteros.Preferences;

public class WatchedItem : Entity<Guid>
{
    public Guid UserPreferenceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;

    protected WatchedItem()
    {
        /* Requerido por Entity Framework Core */
    }

    internal WatchedItem(Guid id, Guid userPreferenceId, string name, string normalizedName) : base(id)
    {
        UserPreferenceId = userPreferenceId;
        Name = name;
        NormalizedName = normalizedName;
    }

    /// <summary>Recorta y colapsa espacios. Es lo que se muestra al usuario.</summary>
    public static string Clean(string? name) =>
        Regex.Replace((name ?? string.Empty).Trim(), @"\s+", " ");

    /// <summary>Clave de comparación: sin acentos ni mayúsculas. "Maní" y "mani" son la misma.</summary>
    public static string Normalize(string cleanName)
    {
        var decomposed = cleanName.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}