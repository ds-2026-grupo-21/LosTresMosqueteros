namespace LosTresMosqueteros.Pantry;

public enum ExpirationState
{
    /// <summary>Sin aviso: sin fecha, consumido o a más de tres días.</summary>
    None = 0,

    /// <summary>Vence dentro de uno a tres días, inclusive.</summary>
    DueSoon = 1,

    /// <summary>Vence hoy (día UTC).</summary>
    DueToday = 2,

    /// <summary>Ya venció.</summary>
    Expired = 3
}