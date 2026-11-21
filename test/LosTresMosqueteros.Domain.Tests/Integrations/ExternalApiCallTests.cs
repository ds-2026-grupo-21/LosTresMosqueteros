using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Integrations;

public class ExternalApiCallTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Guarda_operacion_resultado_duracion_y_momento()
    {
        var call = new ExternalApiCall(Guid.NewGuid(), ExternalApiOperation.TextSearch,
            ExternalApiOutcome.RateLimited, 250, Now);

        call.Operation.ShouldBe(ExternalApiOperation.TextSearch);
        call.Outcome.ShouldBe(ExternalApiOutcome.RateLimited);
        call.DurationMs.ShouldBe(250);
        call.OccurredAt.ShouldBe(Now);
    }

    [Fact]
    public void Acepta_duracion_cero()
    {
        new ExternalApiCall(Guid.NewGuid(), ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Found, 0, Now)
            .DurationMs.ShouldBe(0);
    }

    [Fact]
    public void Rechaza_duracion_negativa()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ExternalApiCall(Guid.NewGuid(),
            ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Found, -1, Now));
    }

    [Fact]
    public void Rechaza_operacion_o_resultado_inexistentes()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ExternalApiCall(Guid.NewGuid(),
            (ExternalApiOperation)99, ExternalApiOutcome.Found, 1, Now));
        Should.Throw<ArgumentOutOfRangeException>(() => new ExternalApiCall(Guid.NewGuid(),
            ExternalApiOperation.BarcodeLookup, (ExternalApiOutcome)99, 1, Now));
    }

    [Fact]
    public void No_guarda_datos_del_usuario_ni_de_la_consulta()
    {
        // Privacidad: el registro solo conoce operación, resultado, duración y momento.
        typeof(ExternalApiCall).GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray()
            .ShouldBe(new[] { "DurationMs", "Id", "OccurredAt", "Operation", "Outcome" });
    }
}
