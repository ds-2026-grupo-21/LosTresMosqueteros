using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros.Integrations;

public interface IIntegrationMetricsAppService : IApplicationService
{
    Task<IntegrationMetricsDto> GetAsync(GetIntegrationMetricsInput input);
}
