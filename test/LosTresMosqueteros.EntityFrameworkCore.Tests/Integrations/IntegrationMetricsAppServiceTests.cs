using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LosTresMosqueteros.Integrations;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Uow;
using Volo.Abp.Validation;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class IntegrationMetricsAppServiceTests
    : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private readonly IIntegrationMetricsAppService _service;
    private readonly IRepository<ExternalApiCall, Guid> _calls;
    private readonly DateTime _nowUtc;

    public IntegrationMetricsAppServiceTests()
    {
        _service = GetRequiredService<IIntegrationMetricsAppService>();
        _calls = GetRequiredService<IRepository<ExternalApiCall, Guid>>();
        _nowUtc = DateTime.SpecifyKind(GetRequiredService<IClock>().Now.ToUniversalTime(), DateTimeKind.Utc);
    }

    [Fact]
    public async Task Agrupa_por_operacion_y_calcula_fallas_y_duraciones_del_periodo()
    {
        await InsertAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Found, 100, _nowUtc.AddHours(-1));
        await InsertAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.NotFound, 300, _nowUtc.AddHours(-2));
        await InsertAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.RateLimited, 50, _nowUtc.AddHours(-3));
        await InsertAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Unavailable, 1000, _nowUtc.AddDays(-2));
        await InsertAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Error, 10, _nowUtc.AddDays(-10)); // fuera de 7 días
        await InsertAsync(ExternalApiOperation.TextSearch, ExternalApiOutcome.Found, 200, _nowUtc.AddMinutes(-30));

        var result = await _service.GetAsync(new GetIntegrationMetricsInput { Days = 7 });

        result.Days.ShouldBe(7);
        result.TotalCalls.ShouldBe(5);
        result.Operations.Select(x => x.Operation)
            .ShouldBe(new[] { ExternalApiOperation.BarcodeLookup, ExternalApiOperation.TextSearch });

        var barcode = result.Operations.Single(x => x.Operation == ExternalApiOperation.BarcodeLookup);
        barcode.TotalCalls.ShouldBe(4);
        barcode.Found.ShouldBe(1);
        barcode.NotFound.ShouldBe(1);
        barcode.RateLimited.ShouldBe(1);
        barcode.Unavailable.ShouldBe(1);
        barcode.Errors.ShouldBe(0);
        barcode.FailureRatePercent.ShouldBe(50);   // límite + no disponible; "no encontrado" no es falla
        barcode.HighFailureRate.ShouldBeTrue();
        barcode.AverageDurationMs.ShouldBe(362.5);
        barcode.MaxDurationMs.ShouldBe(1000);
        barcode.LastCallAt.ShouldNotBeNull();

        var search = result.Operations.Single(x => x.Operation == ExternalApiOperation.TextSearch);
        search.TotalCalls.ShouldBe(1);
        search.FailureRatePercent.ShouldBe(0);
        search.HighFailureRate.ShouldBeFalse();
        search.AverageDurationMs.ShouldBe(200);
    }

    [Fact]
    public async Task Un_periodo_mas_largo_incluye_las_llamadas_anteriores()
    {
        await InsertAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Found, 100, _nowUtc.AddHours(-1));
        await InsertAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Error, 10, _nowUtc.AddDays(-10));

        (await _service.GetAsync(new GetIntegrationMetricsInput { Days = 7 })).TotalCalls.ShouldBe(1);

        var month = await _service.GetAsync(new GetIntegrationMetricsInput { Days = 30 });
        month.TotalCalls.ShouldBe(2);
        month.Operations.Single(x => x.Operation == ExternalApiOperation.BarcodeLookup).Errors.ShouldBe(1);
    }

    [Fact]
    public async Task Sin_llamadas_devuelve_ambas_operaciones_en_cero()
    {
        var result = await _service.GetAsync(new GetIntegrationMetricsInput());

        result.Days.ShouldBe(IntegrationMetricsConsts.DefaultDays);
        result.TotalCalls.ShouldBe(0);
        result.Operations.Count.ShouldBe(2);
        result.Operations.ShouldAllBe(x => x.TotalCalls == 0 && x.AverageDurationMs == null
            && x.MaxDurationMs == null && x.LastCallAt == null && !x.HighFailureRate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(IntegrationMetricsConsts.MaxDays + 1)]
    public async Task Rechaza_periodos_fuera_de_rango(int days)
    {
        await Should.ThrowAsync<AbpValidationException>(
            () => _service.GetAsync(new GetIntegrationMetricsInput { Days = days }));
    }

    [Fact]
    public async Task El_registrador_guarda_la_llamada_en_su_propia_unidad_de_trabajo()
    {
        await GetRequiredService<IExternalApiCallRecorder>()
            .RecordAsync(ExternalApiOperation.TextSearch, ExternalApiOutcome.RateLimited, 1234);

        var saved = (await WithUnitOfWorkAsync(() => _calls.GetListAsync())).ShouldHaveSingleItem();
        saved.Operation.ShouldBe(ExternalApiOperation.TextSearch);
        saved.Outcome.ShouldBe(ExternalApiOutcome.RateLimited);
        saved.DurationMs.ShouldBe(1234);

        var metrics = await _service.GetAsync(new GetIntegrationMetricsInput { Days = 1 });
        metrics.Operations.Single(x => x.Operation == ExternalApiOperation.TextSearch).RateLimited.ShouldBe(1);
    }

    [Fact]
    public async Task Si_no_se_puede_guardar_la_metrica_la_consulta_del_usuario_no_falla()
    {
        var failingRepository = Substitute.For<IRepository<ExternalApiCall, Guid>>();
        failingRepository.InsertAsync(Arg.Any<ExternalApiCall>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExternalApiCall>(new InvalidOperationException("base no disponible")));

        var recorder = new ExternalApiCallRecorder(
            failingRepository,
            GetRequiredService<IUnitOfWorkManager>(),
            GetRequiredService<IClock>(),
            GetRequiredService<IGuidGenerator>(),
            NullLogger<ExternalApiCallRecorder>.Instance);

        await Should.NotThrowAsync(
            () => recorder.RecordAsync(ExternalApiOperation.BarcodeLookup, ExternalApiOutcome.Found, 10));
    }

    private Task InsertAsync(ExternalApiOperation operation, ExternalApiOutcome outcome, int durationMs, DateTime occurredAt) =>
        WithUnitOfWorkAsync(() => _calls.InsertAsync(
            new ExternalApiCall(Guid.NewGuid(), operation, outcome, durationMs, occurredAt)));
}
