using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros.Preferences;

public interface IPreferenceAppService : IApplicationService
{
    Task<UserPreferenceDto> GetAsync();

    Task<UserPreferenceDto> UpdateAsync(UpdateUserPreferenceDto input);
}