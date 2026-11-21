namespace LosTresMosqueteros.Integrations;

public static class IntegrationMetricsConsts
{
    /// <summary>Período por defecto de la consulta, en días.</summary>
    public const int DefaultDays = 7;

    /// <summary>Período máximo de la consulta, en días.</summary>
    public const int MaxDays = 90;

    /// <summary>
    /// Porcentaje de fallas (límite de solicitudes, servicio no disponible o error) a partir del cual
    /// una operación se marca como problemática. "No encontrado" no cuenta como falla.
    /// </summary>
    public const double HighFailureRatePercent = 20;
}
