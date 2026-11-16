using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Users;

namespace LosTresMosqueteros.Preferences;

public interface IFoodWarningEvaluator
{
    Task<FoodWarningDto> EvaluateAsync(string? ingredientes, string? alergenos);
}

/// <summary>
/// Obtiene las preferencias persistidas del usuario de la sesión y aplica la regla de dominio.
/// El propietario sale siempre de la sesión, nunca del cliente.
/// </summary>
public class FoodWarningEvaluator : IFoodWarningEvaluator, ITransientDependency
{
    private readonly IRepository<UserPreference, Guid> _repository;
    private readonly IAsyncQueryableExecuter _executer;
    private readonly ICurrentUser _currentUser;

    public FoodWarningEvaluator(
        IRepository<UserPreference, Guid> repository,
        IAsyncQueryableExecuter executer,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _executer = executer;
        _currentUser = currentUser;
    }

    public async Task<FoodWarningDto> EvaluateAsync(string? ingredientes, string? alergenos)
    {
        var userId = _currentUser.GetId();

        var query = await _repository.WithDetailsAsync(x => x.WatchedItems);
        var preference = await _executer.FirstOrDefaultAsync(query.Where(x => x.UserId == userId));

        var names = preference?.WatchedItems.Select(x => x.Name).ToList() ?? new List<string>();
        var result = FoodPreferenceComparer.Compare(names, ingredientes, alergenos);

        return new FoodWarningDto
        {
            HasPreferences = result.HasPreferences,
            ShowAlerts = preference?.ShowAlerts ?? true,
            Matches = result.Matches
                .Select(x => new FoodWarningMatchDto { Preference = x.Preference, Field = x.Field })
                .ToList(),
            Unverifiable = result.Unverifiable.ToList()
        };
    }
}