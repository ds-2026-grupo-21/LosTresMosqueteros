using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Guids;

namespace LosTresMosqueteros.Preferences;

public class UserPreference : AuditedAggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public bool ShowAlerts { get; private set; }
    public ICollection<WatchedItem> WatchedItems { get; private set; } = new List<WatchedItem>();

    protected UserPreference()
    {
        /* Requerido por Entity Framework Core */
    }

    public UserPreference(Guid id, Guid userId) : base(id)
    {
        UserId = userId;
        ShowAlerts = true; // valor inicial acordado
    }

    public void Update(bool showAlerts, IEnumerable<string?> names, IGuidGenerator guidGenerator)
    {
        var wanted = new Dictionary<string, string>(); // normalizado -> nombre a mostrar

        foreach (var raw in names)
        {
            var clean = WatchedItem.Clean(raw);

            if (clean.Length == 0)
            {
                throw new ArgumentException("No se admiten entradas vacías.", nameof(names));
            }

            if (clean.Length > PreferenceConsts.MaxNameLength)
            {
                throw new ArgumentException(
                    $"Cada entrada puede tener hasta {PreferenceConsts.MaxNameLength} caracteres.", nameof(names));
            }

            // Repetidas (sin distinguir mayúsculas ni acentos): se conserva la primera.
            wanted.TryAdd(WatchedItem.Normalize(clean), clean);
        }

        if (wanted.Count > PreferenceConsts.MaxWatchedItems)
        {
            throw new ArgumentException(
                $"Se admiten hasta {PreferenceConsts.MaxWatchedItems} entradas.", nameof(names));
        }

        ShowAlerts = showAlerts;

        // Se actualiza por diferencia (no se borra todo y se reinserta) para no chocar con el índice único.
        foreach (var existing in WatchedItems.Where(x => !wanted.ContainsKey(x.NormalizedName)).ToList())
        {
            WatchedItems.Remove(existing);
        }

        var current = WatchedItems.Select(x => x.NormalizedName).ToHashSet();
        foreach (var (normalized, name) in wanted.Where(x => !current.Contains(x.Key)))
        {
            WatchedItems.Add(new WatchedItem(guidGenerator.Create(), Id, name, normalized));
        }
    }
}