using System;

namespace LosTresMosqueteros.Preferences;

public static class PreferencePhoto
{
    public const int MaxBytes = 80000;
    public static bool IsValid(string? dataUrl)
    {
        if (dataUrl == null) return true;
        if (dataUrl.Length > 107000) return false;
        var png = dataUrl.StartsWith("data:image/png;base64,", StringComparison.Ordinal);
        var jpeg = dataUrl.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal);
        if (!png && !jpeg) return false;
        try
        {
            var bytes = Convert.FromBase64String(dataUrl[(dataUrl.IndexOf(',') + 1)..]);
            if (bytes.Length > MaxBytes || bytes.Length < 12) return false;
            return png ? bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
                : bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255;
        }
        catch (FormatException) { return false; }
    }
}
