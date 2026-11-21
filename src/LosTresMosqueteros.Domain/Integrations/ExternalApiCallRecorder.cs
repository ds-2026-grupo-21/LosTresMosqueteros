using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace LosTresMosqueteros.Integrations;

public class ExternalApiCallRecorder : IExternalApiCallRecorder, ITransientDependency
{
    private readonly IRepository<ExternalApiCall, Guid> _calls;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IClock _clock;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ILogger<ExternalApiCallRecorder> _logger;

    public ExternalApiCallRecorder(
        IRepository<ExternalApiCall, Guid> calls,
        IUnitOfWorkManager unitOfWorkManager,
        IClock clock,
        IGuidGenerator guidGenerator,
        ILogger<ExternalApiCallRecorder> logger)
    {
        _calls = calls;
        _unitOfWorkManager = unitOfWorkManager;
        _clock = clock;
        _guidGenerator = guidGenerator;
        _logger = logger;
    }

    public async Task RecordAsync(ExternalApiOperation operation, ExternalApiOutcome outcome, long durationMs)
    {
        try
        {
            var call = new ExternalApiCall(
                _guidGenerator.Create(),
                operation,
                outcome,
                (int)Math.Clamp(durationMs, 0, int.MaxValue),
                DateTime.SpecifyKind(_clock.Now.ToUniversalTime(), DateTimeKind.Utc));

            // Unidad de trabajo propia: la métrica se guarda aunque la operación del usuario falle después,
            // y un error al guardarla no afecta la respuesta al usuario.
            using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
            await _calls.InsertAsync(call);
            await unitOfWork.CompleteAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "No se pudo registrar la métrica de {Operation} con resultado {Outcome}.", operation, outcome);
        }
    }
}
