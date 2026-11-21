using System.Diagnostics;
using System.Threading.Tasks;
using LosTresMosqueteros.Productos;

namespace LosTresMosqueteros.Integrations;

/// <summary>
/// Decorador del cliente de búsqueda por texto: mide la duración y registra el resultado de cada llamada,
/// sin guardar el texto buscado.
/// </summary>
public class MeteredExternalProductSearchClient : IExternalProductSearchClient
{
    private readonly IExternalProductSearchClient _inner;
    private readonly IExternalApiCallRecorder _recorder;

    public MeteredExternalProductSearchClient(IExternalProductSearchClient inner, IExternalApiCallRecorder recorder)
    {
        _inner = inner;
        _recorder = recorder;
    }

    public async Task<ExternalProductSearchResultDto> SearchAsync(string text, int page, int pageSize)
    {
        var stopwatch = Stopwatch.StartNew();
        ExternalProductSearchResultDto result;
        try
        {
            result = await _inner.SearchAsync(text, page, pageSize);
        }
        catch
        {
            await _recorder.RecordAsync(ExternalApiOperation.TextSearch, ExternalApiOutcome.Error,
                stopwatch.ElapsedMilliseconds);
            throw;
        }

        await _recorder.RecordAsync(ExternalApiOperation.TextSearch,
            ExternalApiOutcomeMapper.FromResultado(result.Resultado), stopwatch.ElapsedMilliseconds);
        return result;
    }
}
