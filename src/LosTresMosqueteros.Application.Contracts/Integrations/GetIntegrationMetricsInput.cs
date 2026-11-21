using System.ComponentModel.DataAnnotations;

namespace LosTresMosqueteros.Integrations;

public class GetIntegrationMetricsInput
{
    /// <summary>Cantidad de días hacia atrás desde ahora (UTC).</summary>
    [Range(1, IntegrationMetricsConsts.MaxDays)]
    public int Days { get; set; } = IntegrationMetricsConsts.DefaultDays;
}
