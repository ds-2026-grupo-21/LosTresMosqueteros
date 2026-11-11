using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LosTresMosqueteros.Preferences;

[Authorize]
public class PreferenceAppService : LosTresMosqueterosAppService, IPreferenceAppService
{
    private readonly IRepository<UserPreference, Guid> _repository;

    public PreferenceAppService(IRepository<UserPreference, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<UserPreferenceDto> GetAsync()
    {
        var preference = await FindOwnAsync();

        // Sin fila todavía: se devuelven los valores iniciales, sin crear nada.
        return preference == null
            ? new UserPreferenceDto { ShowAlerts = true }
            : MapToDto(preference);
    }

    public async Task<UserPreferenceDto> UpdateAsync(UpdateUserPreferenceDto input)
    {
        var preference = await FindOwnAsync();

        if (preference == null)
        {
            // El propietario sale siempre de la sesión, nunca del cliente.
            preference = new UserPreference(GuidGenerator.Create(), CurrentUser.GetId());
            preference.Update(input.ShowAlerts, input.WatchedItems, GuidGenerator);
            await _repository.InsertAsync(preference, autoSave: true);
        }
        else
        {
            preference.Update(input.ShowAlerts, input.WatchedItems, GuidGenerator);
            await _repository.UpdateAsync(preference, autoSave: true);
        }

        return MapToDto(preference);
    }

    private async Task<UserPreference?> FindOwnAsync()
    {
        var userId = CurrentUser.GetId();
        var query = await _repository.WithDetailsAsync(x => x.WatchedItems);
        return await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.UserId == userId));
    }

    private static UserPreferenceDto MapToDto(UserPreference preference) => new()
    {
        ShowAlerts = preference.ShowAlerts,
        WatchedItems = preference.WatchedItems.Select(x => x.Name).OrderBy(x => x).ToList()
    };
}