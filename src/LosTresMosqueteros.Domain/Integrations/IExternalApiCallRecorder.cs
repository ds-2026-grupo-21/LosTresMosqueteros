using System.Threading.Tasks;

namespace LosTresMosqueteros.Integrations;

public interface IExternalApiCallRecorder
{
    /// <summary>
    /// Registra una llamada al proveedor externo. Nunca lanza excepciones: si el registro falla,
    /// la consulta del usuario continúa y el problema queda en el log.
    /// </summary>
    Task RecordAsync(ExternalApiOperation operation, ExternalApiOutcome outcome, long durationMs);
}
