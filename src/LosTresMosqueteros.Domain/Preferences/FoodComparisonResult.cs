using System.Collections.Generic;

namespace LosTresMosqueteros.Preferences;

/// <summary>Una preferencia del usuario que apareció en un campo del producto.</summary>
public sealed record FoodMatch(string Preference, FoodField Field);

public sealed class FoodComparisonResult
{
    public FoodComparisonResult(
        bool hasPreferences,
        IReadOnlyList<FoodMatch> matches,
        IReadOnlyList<FoodField> unverifiable)
    {
        HasPreferences = hasPreferences;
        Matches = matches;
        Unverifiable = unverifiable;
    }

    /// <summary>False: el usuario no tiene elementos configurados para comparar.</summary>
    public bool HasPreferences { get; }

    /// <summary>Coincidencias encontradas en los datos disponibles.</summary>
    public IReadOnlyList<FoodMatch> Matches { get; }

    /// <summary>Campos que faltan o no se pueden interpretar. No equivalen a "sin riesgo".</summary>
    public IReadOnlyList<FoodField> Unverifiable { get; }

    public bool HasMatches => Matches.Count > 0;

    /// <summary>True: ingredientes y alérgenos estaban disponibles para comparar.</summary>
    public bool IsComplete => Unverifiable.Count == 0;
}