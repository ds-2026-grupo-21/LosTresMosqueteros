using LosTresMosqueteros.Productos;

namespace LosTresMosqueteros.Integrations;

public static class ExternalApiOutcomeMapper
{
    public static ExternalApiOutcome FromResultado(ResultadoConsultaExterna resultado) => resultado switch
    {
        ResultadoConsultaExterna.Encontrado => ExternalApiOutcome.Found,
        ResultadoConsultaExterna.NoEncontrado => ExternalApiOutcome.NotFound,
        ResultadoConsultaExterna.LimiteDeSolicitudesExcedido => ExternalApiOutcome.RateLimited,
        ResultadoConsultaExterna.ServicioNoDisponible => ExternalApiOutcome.Unavailable,
        _ => ExternalApiOutcome.Error
    };
}
