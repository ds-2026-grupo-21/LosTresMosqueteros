using System;

namespace LosTresMosqueteros.Pantry;

/// <summary>
/// Regla única de vencimiento, compartida por el worker y por la API de despensa.
/// El día de referencia es UTC y el umbral incluye el día límite: vencidos, hoy y hasta +3.
/// Los ítems consumidos o sin fecha no tienen aviso activo.
/// </summary>
public static class ExpirationRule
{
    public const int ThresholdDays = 3;

    /// <summary>Días hasta el vencimiento. Negativo si ya venció; null si no hay fecha.</summary>
    public static int? DaysRemaining(DateOnly? expirationDate, DateOnly todayUtc) =>
        expirationDate?.DayNumber - todayUtc.DayNumber;

    public static ExpirationState GetState(
        PantryItemStatus status, DateOnly? expirationDate, DateOnly todayUtc)
    {
        if (status != PantryItemStatus.Active || expirationDate is null)
        {
            return ExpirationState.None;
        }

        var days = expirationDate.Value.DayNumber - todayUtc.DayNumber;

        return days switch
        {
            < 0 => ExpirationState.Expired,
            0 => ExpirationState.DueToday,
            <= ThresholdDays => ExpirationState.DueSoon,
            _ => ExpirationState.None
        };
    }

    public static bool ShouldWarn(PantryItemStatus status, DateOnly? expirationDate, DateOnly todayUtc) =>
        GetState(status, expirationDate, todayUtc) != ExpirationState.None;
}