using System;
using System.Collections.Generic;

namespace LosTresMosqueteros.Integrations;

public class IntegrationMetricsDto
{
    /// <summary>Inicio del período consultado, en UTC.</summary>
    public DateTime From { get; set; }

    /// <summary>Fin del período consultado, en UTC.</summary>
    public DateTime To { get; set; }

    public int Days { get; set; }

    public int TotalCalls { get; set; }

    /// <summary>Una fila por operación, siempre presente aunque no haya llamadas.</summary>
    public List<OperationMetricsDto> Operations { get; set; } = new();
}
