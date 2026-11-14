using System.Collections.Generic;

namespace LosTresMosqueteros.Preferences;

/// <summary>
/// Evaluación de un producto contra las preferencias del usuario autenticado.
/// Se calcula al momento y no se guarda: no pertenece al producto compartido.
/// </summary>
public class FoodWarningDto
{
    /// <summary>false = el usuario no tiene elementos configurados para comparar.</summary>
    public bool HasPreferences { get; set; }

    /// <summary>Preferencia de presentación; el servidor siempre informa los datos.</summary>
    public bool ShowAlerts { get; set; } = true;

    public List<FoodWarningMatchDto> Matches { get; set; } = new();

    /// <summary>Campos ausentes o ilegibles: no se pudieron comparar.</summary>
    public List<FoodField> Unverifiable { get; set; } = new();
}

public class FoodWarningMatchDto
{
    public string Preference { get; set; } = string.Empty;
    public FoodField Field { get; set; }
}