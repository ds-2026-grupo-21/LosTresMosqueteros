using System;
using System.Threading;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Xunit;

namespace LosTresMosqueteros;

public class ExpirationWarningWorkerTests
{
    [Fact]
    public async Task Deberia_Usar_Un_Solo_Dia_Utc_Para_Toda_La_Ejecucion()
    {
        var clock = Substitute.For<IClock>();
        // Corresponde a las 21:30 del día anterior en Argentina.
        clock.Now.Returns(new DateTime(2026, 10, 22, 0, 30, 0, DateTimeKind.Utc));
        var manager = CreateManager();
        using var provider = CreateProvider(clock, manager);
        var worker = new TestWorker(provider);

        await worker.RunOnceAsync(provider);

        await manager.Received(1).ProcessAsync(new DateOnly(2026, 10, 22), Arg.Any<CancellationToken>());
        _ = clock.Received(1).Now;
    }

    [Fact]
    public async Task Deberia_Resolver_El_Manager_Desde_El_Alcance_De_Cada_Ejecucion()
    {
        var clock = Substitute.For<IClock>();
        clock.Now.Returns(new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc));
        var first = CreateManager();
        var second = CreateManager();
        using var firstProvider = CreateProvider(clock, first);
        using var secondProvider = CreateProvider(clock, second);
        var worker = new TestWorker(firstProvider);

        await worker.RunOnceAsync(firstProvider);
        await worker.RunOnceAsync(secondProvider);

        await first.Received(1).ProcessAsync(new DateOnly(2026, 10, 22), Arg.Any<CancellationToken>());
        await second.Received(1).ProcessAsync(new DateOnly(2026, 10, 22), Arg.Any<CancellationToken>());
    }

    private static ExpirationWarningManager CreateManager()
    {
        return Substitute.For<ExpirationWarningManager>(
            Substitute.For<IRepository<PantryItem, Guid>>(),
            Substitute.For<IRepository<ExpirationWarning, Guid>>(),
            Substitute.For<IUnitOfWorkManager>());
    }

    private static ServiceProvider CreateProvider(IClock clock, ExpirationWarningManager manager)
    {
        return new ServiceCollection().AddLogging().AddSingleton(clock).AddSingleton(manager).BuildServiceProvider();
    }

    private class TestWorker : ExpirationWarningWorker
    {
        public TestWorker(IServiceProvider provider) : base(new AbpAsyncTimer(), provider.GetRequiredService<IServiceScopeFactory>())
        {
            LazyServiceProvider = new AbpLazyServiceProvider(provider);
        }

        public Task RunOnceAsync(IServiceProvider provider)
        {
            return DoWorkAsync(new PeriodicBackgroundWorkerContext(provider));
        }
    }
}
