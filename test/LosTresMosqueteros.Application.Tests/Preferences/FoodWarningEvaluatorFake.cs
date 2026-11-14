using System.Threading.Tasks;

namespace LosTresMosqueteros.Preferences;

/// <summary>
/// Evaluador de prueba para tests que no verifican advertencias.
/// Devuelve "sin preferencias" y no accede a la base de datos.
/// </summary>
public class FoodWarningEvaluatorFake : IFoodWarningEvaluator
{
    public Task<FoodWarningDto> EvaluateAsync(string? ingredientes, string? alergenos)
        => Task.FromResult(new FoodWarningDto { HasPreferences = false, ShowAlerts = true });
}