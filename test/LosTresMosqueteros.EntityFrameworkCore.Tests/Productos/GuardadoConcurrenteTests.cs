using System;
using System.Threading;
using System.Threading.Tasks;
using LosTresMosqueteros.Productos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Uow;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class GuardadoConcurrenteTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    [Fact]
    public async Task Dos_guardados_que_leen_codigo_inexistente_recuperan_la_colision_sin_duplicar()
    {
        const string code = "0000000000456";
        var coordination = new InsertCoordination();
        var catalog = Substitute.For<IExternalProductCatalogClient>();
        catalog.GetByBarcodeAsync(code).Returns(new ExternalProductDto
        {
            Resultado = ResultadoConsultaExterna.Encontrado,
            CodigoBarras = code,
            Nombre = "Producto concurrente"
        });

        async Task<GuardarProductoExternoResultadoDto> SaveAsync()
        {
            using var scope = ServiceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var units = services.GetRequiredService<IUnitOfWorkManager>();
            using var request = units.Begin(requiresNew: true, isTransactional: false);
            var repository = new CoordinatedRepository(
                services.GetRequiredService<IDbContextProvider<LosTresMosqueterosDbContext>>(), units, coordination)
            {
                ServiceProvider = services,
                LazyServiceProvider = services.GetRequiredService<IAbpLazyServiceProvider>()
            };
            var service = new GuardarProductoExternoAppService(repository, catalog,
                services.GetRequiredService<IGuidGenerator>(), services.GetRequiredService<IObjectMapper>());
            var result = await service.SaveAsync(new() { CodigoBarras = code });
            await request.CompleteAsync();
            coordination.FirstCommitted.TrySetResult();
            return result;
        }

        var results = await Task.WhenAll(SaveAsync(), SaveAsync());
        results[0].Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        results[1].Resultado.ShouldBe(ResultadoConsultaExterna.Encontrado);
        results[0].Producto!.Id.ShouldBe(results[1].Producto!.Id);
        var repository = GetRequiredService<IProductoRepository>();
        var stored = await WithUnitOfWorkAsync(() => repository.GetListAsync(x => x.CodigoBarras == code));
        stored.ShouldHaveSingleItem().Id.ShouldBe(results[0].Producto!.Id);
        coordination.InsertAttempts.ShouldBe(2);
        coordination.UniqueConflicts.ShouldBe(1);
        await catalog.Received(2).GetByBarcodeAsync(code);
    }

    private sealed class InsertCoordination
    {
        public int InsertAttempts;
        public int UniqueConflicts;
        public TaskCompletionSource BothReady { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource FirstCommitted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [DisableConventionalRegistration]
    private sealed class CoordinatedRepository(
        IDbContextProvider<LosTresMosqueterosDbContext> provider, IUnitOfWorkManager units,
        InsertCoordination coordination) : ProductoRepository(provider, units)
    {
        public override async Task<Producto> InsertAsync(Producto entity, bool autoSave = false,
            CancellationToken cancellationToken = default)
        {
            var position = Interlocked.Increment(ref coordination.InsertAttempts);
            if (position == 2) coordination.BothReady.TrySetResult();
            await coordination.BothReady.Task.WaitAsync(TimeSpan.FromSeconds(30));
            // Ambos ya leyeron que no existe. La segunda escritura ocurre tras confirmar la primera.
            if (position == 2) await coordination.FirstCommitted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            try
            {
                return await base.InsertAsync(entity, autoSave, cancellationToken);
            }
            catch (DbUpdateException)
            {
                Interlocked.Increment(ref coordination.UniqueConflicts);
                throw;
            }
        }
    }
}
