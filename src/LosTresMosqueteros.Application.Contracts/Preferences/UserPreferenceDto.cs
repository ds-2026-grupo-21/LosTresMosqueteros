using System.Collections.Generic;

namespace LosTresMosqueteros.Preferences;

public class UserPreferenceDto
{
    public bool ShowAlerts { get; set; }
    public string? PhotoDataUrl { get; set; }
    public List<string> WatchedItems { get; set; } = new();
}
