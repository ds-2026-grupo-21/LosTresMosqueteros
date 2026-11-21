using System;

namespace LosTresMosqueteros.Integrations;

public class OperationMetricsDto
{
    public ExternalApiOperation Operation { get; set; }

    public int TotalCalls { get; set; }

    public int Found { get; set; }

    public int NotFound { get; set; }

    public int RateLimited { get; set; }

    public int Unavailable { get; set; }

    public int Errors { get; set; }

    /// <summary>Porcentaje de llamadas con límite de solicitudes, servicio no disponible o error.</summary>
    public double FailureRatePercent { get; set; }

    /// <summary>True si el porcentaje de fallas alcanza el umbral configurado.</summary>
    public bool HighFailureRate { get; set; }

    /// <summary>Duración promedio en milisegundos; null si no hubo llamadas.</summary>
    public double? AverageDurationMs { get; set; }

    /// <summary>Duración máxima en milisegundos; null si no hubo llamadas.</summary>
    public int? MaxDurationMs { get; set; }

    /// <summary>Última llamada del período, en UTC; null si no hubo llamadas.</summary>
    public DateTime? LastCallAt { get; set; }
}
