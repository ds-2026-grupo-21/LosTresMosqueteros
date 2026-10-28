using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Timing;

namespace LosTresMosqueteros.Pantry;

public class ExpirationWarningWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ExpirationWarningWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 60_000; // Un minuto entre ejecuciones; el umbral del aviso es de tres días.
        Timer.RunOnStart = true;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
        var manager = workerContext.ServiceProvider.GetRequiredService<ExpirationWarningManager>();
        var todayUtc = DateOnly.FromDateTime(clock.Now.ToUniversalTime());

        await manager.ProcessAsync(todayUtc, workerContext.CancellationToken);
        Logger.LogInformation("Procesamiento de vencimientos completado para {TodayUtc}.", todayUtc);
    }
}
