using System;
using SkiaSharp;

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
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec == null || codec.FrameCount > 1) return false;
            if (png ? codec.EncodedFormat != SKEncodedImageFormat.Png : codec.EncodedFormat != SKEncodedImageFormat.Jpeg)
                return false;
            var info = codec.Info;
            if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > 16000000) return false;
            using var bitmap = new SKBitmap(new SKImageInfo(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
            return bitmap.GetPixels() != IntPtr.Zero && codec.GetPixels(bitmap.Info, bitmap.GetPixels()) == SKCodecResult.Success;
        }
        catch (FormatException) { return false; }
    }
}
