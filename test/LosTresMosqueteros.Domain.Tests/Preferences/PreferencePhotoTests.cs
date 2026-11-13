using System;
using SkiaSharp;
using Shouldly;
using Volo.Abp.Guids;
using Xunit;

namespace LosTresMosqueteros.Preferences;

public class PreferencePhotoTests
{
    public const string Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAALSURBVBhXY2BABwAAEgABp3qZbgAAAABJRU5ErkJggg==";
    [Fact]
    public void Foto_opcional_se_guarda_y_se_quita()
    {
        var p = new UserPreference(Guid.NewGuid(), Guid.NewGuid());
        p.Update(true, new[] { "Gluten" }, SimpleGuidGenerator.Instance, Png);
        p.PhotoDataUrl.ShouldBe(Png);
        p.Update(true, Array.Empty<string>(), SimpleGuidGenerator.Instance);
        p.PhotoDataUrl.ShouldBeNull();
    }
    [Theory]
    [InlineData("data:image/svg+xml;base64,PHN2Zz4=")]
    [InlineData("https://example.com/photo.png")]
    [InlineData("data:image/png;base64,invalid")]
    [InlineData("data:image/png;base64,YWJjZGVmZ2hpamtsbW5vcA==")]
    public void Rechaza_datos_que_no_son_fotos_admitidas(string value) => PreferencePhoto.IsValid(value).ShouldBeFalse();
    [Fact]
    public void Rechaza_foto_que_supera_el_limite() =>
        PreferencePhoto.IsValid("data:image/png;base64," + Convert.ToBase64String(new byte[80001])).ShouldBeFalse();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Acepta_imagenes_completas_y_rechaza_cabeceras_sin_imagen(bool png)
    {
        using var image = new SKBitmap(2, 2);
        image.Erase(SKColors.White);
        using var encoded = image.Encode(png ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg, 100);
        var prefix = png ? "data:image/png;base64," : "data:image/jpeg;base64,";
        var bytes = encoded.ToArray();
        PreferencePhoto.IsValid(prefix + Convert.ToBase64String(bytes)).ShouldBeTrue();
        PreferencePhoto.IsValid(prefix + Convert.ToBase64String(bytes[..12])).ShouldBeFalse();
        PreferencePhoto.IsValid(prefix + Convert.ToBase64String(bytes[..(bytes.Length / 2)])).ShouldBeFalse();
        var otherPrefix = png ? "data:image/jpeg;base64," : "data:image/png;base64,";
        PreferencePhoto.IsValid(otherPrefix + Convert.ToBase64String(bytes)).ShouldBeFalse();
    }

    [Fact]
    public void Foto_corrupta_no_modifica_lo_guardado()
    {
        var p = new UserPreference(Guid.NewGuid(), Guid.NewGuid());
        p.Update(true, new[] { "Gluten" }, SimpleGuidGenerator.Instance, Png);
        var truncated = "data:image/png;base64," + Convert.ToBase64String(new byte[] {137,80,78,71,13,10,26,10,0,0,0,0});
        Should.Throw<ArgumentException>(() => p.Update(false, new[] { "Maní" }, SimpleGuidGenerator.Instance, truncated));
        p.ShowAlerts.ShouldBeTrue();
        p.PhotoDataUrl.ShouldBe(Png);
        p.WatchedItems.ShouldHaveSingleItem().Name.ShouldBe("Gluten");
    }
}
