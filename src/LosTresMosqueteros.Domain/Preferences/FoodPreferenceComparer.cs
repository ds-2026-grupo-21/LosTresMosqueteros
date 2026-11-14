using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LosTresMosqueteros.Preferences;

/// <summary>
/// Regla reutilizable que compara las preferencias de un usuario con los ingredientes y
/// alérgenos de un producto. No depende de HTTP, Angular ni EF Core.
///
/// Normalización: sin mayúsculas ni acentos, espacios colapsados, prefijo de idioma de las
/// etiquetas del proveedor ("en:milk" -> "milk") y todo carácter que no sea letra o número
/// pasa a ser un separador ("tree-nuts" -> "tree nuts").
/// Coincidencia: por palabras completas o secuencias de palabras completas, para evitar
/// falsos positivos por fragmento ("mani" no coincide con "manitas").
/// No se traducen ni se infieren equivalencias ("peanuts" no es "maní") ni plurales.
/// </summary>
public static class FoodPreferenceComparer
{
    private static readonly Regex LanguagePrefix = new(@"\b[a-z]{2,3}:(?=[a-z0-9])", RegexOptions.Compiled);
    private static readonly Regex NonAlphanumeric = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    public static FoodComparisonResult Compare(
        IEnumerable<string?>? preferences,
        string? ingredients,
        string? allergens)
    {
        var wanted = new List<(string Display, string Key)>();
        var seen = new HashSet<string>();

        foreach (var raw in preferences ?? Enumerable.Empty<string?>())
        {
            var display = WatchedItem.Clean(raw);
            var key = Tokenize(display);

            // Una entrada que no se puede interpretar o repetida no genera comparación.
            if (key.Length > 0 && seen.Add(key))
            {
                wanted.Add((display, key));
            }
        }

        var fields = new[]
        {
            (Field: FoodField.Ingredients, Text: Tokenize(ingredients)),
            (Field: FoodField.Allergens, Text: Tokenize(allergens))
        };

        // Un dato ausente o ilegible es "no verificable", nunca "sin riesgo".
        var unverifiable = fields.Where(x => x.Text.Length == 0).Select(x => x.Field).ToList();

        var matches = new List<FoodMatch>();
        foreach (var (display, key) in wanted)
        {
            foreach (var (field, text) in fields.Where(x => x.Text.Length > 0))
            {
                if ($" {text} ".Contains($" {key} ", System.StringComparison.Ordinal))
                {
                    matches.Add(new FoodMatch(display, field));
                }
            }
        }

        return new FoodComparisonResult(wanted.Count > 0, matches, unverifiable);
    }

    private static string Tokenize(string? value)
    {
        var normalized = WatchedItem.Normalize(WatchedItem.Clean(value));
        normalized = LanguagePrefix.Replace(normalized, string.Empty);
        return NonAlphanumeric.Replace(normalized, " ").Trim();
    }
}