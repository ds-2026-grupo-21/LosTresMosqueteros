using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LosTresMosqueteros.Productos;

namespace LosTresMosqueteros.Integrations;

/// <summary>
/// Decorador del cliente de consulta por código: mide la duración y registra el resultado de cada llamada,
/// sin modificar la respuesta ni los datos consultados.
/// </summary>
public class MeteredExternalProductCatalogClient : IExternalProductCatalogClient
{
    private readonly IExternalProductCatalogClient _inner;
    private readonly IExternalApiCallRecorder _recorder;

    public MeteredExternalProductCatalogClient(IExternalProductCatalogClient inner, IExternalApiCallRecorder recorder)
    {
        _inner = inner;
        _recorder = recorder;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        var stopwatch = Stopwatch.StartNew();
        ExternalProductDto? result;
        try
        {
            result = await _inner.GetByBarcodeAsync(barcode);
        }
        catch
        {
            await _recorder.RecordAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Error,
                stopwatch.ElapsedMilliseconds);
            throw;
        }

        var outcome = result is null
            ? ExternalApiOutcome.NotFound
            : ExternalApiOutcomeMapper.FromResultado(result.Resultado);
        await _recorder.RecordAsync(ExternalApiOperation.BarcodeLookup, outcome, stopwatch.ElapsedMilliseconds);
        return result;
    }
}
