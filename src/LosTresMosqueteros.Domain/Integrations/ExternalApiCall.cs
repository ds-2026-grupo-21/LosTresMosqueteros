using System;
using Volo.Abp.Domain.Entities;

namespace LosTresMosqueteros.Integrations;

/// <summary>
/// Registro de una llamada al proveedor externo. Guarda solo operación, resultado, duración y momento:
/// no guarda usuario, código o texto consultado, tokens ni la respuesta del proveedor.
/// </summary>
public class ExternalApiCall : Entity<Guid>
{
    public ExternalApiOperation Operation { get; private set; }

    public ExternalApiOutcome Outcome { get; private set; }

    /// <summary>Duración de la llamada en milisegundos.</summary>
    public int DurationMs { get; private set; }

    /// <summary>Momento de la llamada, en UTC.</summary>
    public DateTime OccurredAt { get; private set; }

    protected ExternalApiCall()
    {
        /* Requerido por Entity Framework Core */
    }

    public ExternalApiCall(
        Guid id,
        ExternalApiOperation operation,
        ExternalApiOutcome outcome,
        int durationMs,
        DateTime occurredAt) : base(id)
    {
        if (!Enum.IsDefined(operation))
        {
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        if (durationMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMs), "La duración no puede ser negativa.");
        }

        Operation = operation;
        Outcome = outcome;
        DurationMs = durationMs;
        OccurredAt = occurredAt;
    }
}
