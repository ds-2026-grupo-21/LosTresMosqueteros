using System;
using Shouldly;
using Volo.Abp.Guids;
using Xunit;

namespace LosTresMosqueteros.Preferences;

public class PreferencePhotoTests
{
    public const string Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aL1sAAAAASUVORK5CYII=";
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
}
