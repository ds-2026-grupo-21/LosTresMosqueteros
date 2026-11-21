using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LosTresMosqueteros.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace LosTresMosqueteros.Integrations;

/// <summary>
/// Métricas agregadas de las llamadas al proveedor externo (HU-05). Solo para administradores.
/// Se agregan en la base: no se devuelven registros individuales.
/// </summary>
[Authorize(LosTresMosqueterosPermissions.Administration.IntegrationMetrics)]
public class IntegrationMetricsAppService : LosTresMosqueterosAppService, IIntegrationMetricsAppService
{
    private readonly IRepository<ExternalApiCall, Guid> _calls;

    public IntegrationMetricsAppService(IRepository<ExternalApiCall, Guid> calls)
    {
        _calls = calls;
    }

    public async Task<IntegrationMetricsDto> GetAsync(GetIntegrationMetricsInput input)
    {
        var to = DateTime.SpecifyKind(Clock.Now.ToUniversalTime(), DateTimeKind.Utc);
        var from = to.AddDays(-input.Days);

        var query = (await _calls.GetQueryableAsync())
            .Where(x => x.OccurredAt >= from && x.OccurredAt <= to)
            .GroupBy(x => x.Operation)
            .Select(g => new OperationAggregate
            {
                Operation = g.Key,
                TotalCalls = g.Count(),
                Found = g.Count(x => x.Outcome == ExternalApiOutcome.Found),
                NotFound = g.Count(x => x.Outcome == ExternalApiOutcome.NotFound),
                RateLimited = g.Count(x => x.Outcome == ExternalApiOutcome.RateLimited),
                Unavailable = g.Count(x => x.Outcome == ExternalApiOutcome.Unavailable),
                Errors = g.Count(x => x.Outcome == ExternalApiOutcome.Error),
                AverageDurationMs = g.Average(x => (double)x.DurationMs),
                MaxDurationMs = g.Max(x => x.DurationMs),
                LastCallAt = g.Max(x => x.OccurredAt)
            });

        var aggregates = (await AsyncExecuter.ToListAsync(query)).ToDictionary(x => x.Operation);

        var operations = new List<OperationMetricsDto>();
        foreach (var operation in Enum.GetValues<ExternalApiOperation>())
        {
            operations.Add(aggregates.TryGetValue(operation, out var aggregate)
                ? ToDto(aggregate)
                : new OperationMetricsDto { Operation = operation });
        }

        return new IntegrationMetricsDto
        {
            From = from,
            To = to,
            Days = input.Days,
            TotalCalls = operations.Sum(x => x.TotalCalls),
            Operations = operations
        };
    }

    private static OperationMetricsDto ToDto(OperationAggregate aggregate)
    {
        var failures = aggregate.RateLimited + aggregate.Unavailable + aggregate.Errors;
        var failureRate = aggregate.TotalCalls == 0
            ? 0
            : Math.Round(failures * 100.0 / aggregate.TotalCalls, 1);

        return new OperationMetricsDto
        {
            Operation = aggregate.Operation,
            TotalCalls = aggregate.TotalCalls,
            Found = aggregate.Found,
            NotFound = aggregate.NotFound,
            RateLimited = aggregate.RateLimited,
            Unavailable = aggregate.Unavailable,
            Errors = aggregate.Errors,
            FailureRatePercent = failureRate,
            HighFailureRate = aggregate.TotalCalls > 0 && failureRate >= IntegrationMetricsConsts.HighFailureRatePercent,
            AverageDurationMs = Math.Round(aggregate.AverageDurationMs, 1),
            MaxDurationMs = aggregate.MaxDurationMs,
            LastCallAt = DateTime.SpecifyKind(aggregate.LastCallAt, DateTimeKind.Utc)
        };
    }

    private sealed class OperationAggregate
    {
        public ExternalApiOperation Operation { get; set; }
        public int TotalCalls { get; set; }
        public int Found { get; set; }
        public int NotFound { get; set; }
        public int RateLimited { get; set; }
        public int Unavailable { get; set; }
        public int Errors { get; set; }
        public double AverageDurationMs { get; set; }
        public int MaxDurationMs { get; set; }
        public DateTime LastCallAt { get; set; }
    }
}
