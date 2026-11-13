using System;
using System.Linq;
using Shouldly;
using Volo.Abp.Guids;
using Xunit;

namespace LosTresMosqueteros.Preferences;

public class UserPreferenceTests
{
    private static readonly IGuidGenerator Guids = SimpleGuidGenerator.Instance;

    private static UserPreference NewPreference() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Una_preferencia_nueva_muestra_alertas_y_no_tiene_entradas()
    {
        var preference = NewPreference();
        preference.ShowAlerts.ShouldBeTrue();
        preference.WatchedItems.ShouldBeEmpty();
    }

    [Fact]
    public void Guarda_las_entradas_recortadas_y_con_espacios_colapsados()
    {
        var preference = NewPreference();
        preference.Update(false, new[] { "  Maní   tostado " }, Guids);

        preference.ShowAlerts.ShouldBeFalse();
        preference.WatchedItems.Single().Name.ShouldBe("Maní tostado");
    }

    [Fact]
    public void Las_repetidas_sin_importar_mayusculas_ni_acentos_se_guardan_una_vez()
    {
        var preference = NewPreference();
        preference.Update(true, new[] { "Maní", "MANI", " mani ", "Gluten" }, Guids);

        preference.WatchedItems.Count.ShouldBe(2);
        preference.WatchedItems.Select(x => x.Name).ShouldContain("Maní"); // se conserva la primera
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Rechaza_entradas_vacias(string? entry)
    {
        var preference = NewPreference();
        Should.Throw<ArgumentException>(() => preference.Update(true, new[] { entry }, Guids));
    }

    [Fact]
    public void Rechaza_una_entrada_mas_larga_que_el_maximo()
    {
        var preference = NewPreference();
        var tooLong = new string('a', PreferenceConsts.MaxNameLength + 1);
        Should.Throw<ArgumentException>(() => preference.Update(true, new[] { tooLong }, Guids));
    }

    [Fact]
    public void Acepta_una_entrada_del_largo_maximo()
    {
        var preference = NewPreference();
        preference.Update(true, new[] { new string('a', PreferenceConsts.MaxNameLength) }, Guids);
        preference.WatchedItems.Count.ShouldBe(1);
    }

    [Fact]
    public void Rechaza_mas_entradas_que_el_maximo()
    {
        var preference = NewPreference();
        var tooMany = Enumerable.Range(0, PreferenceConsts.MaxWatchedItems + 1).Select(i => $"item {i}");
        Should.Throw<ArgumentException>(() => preference.Update(true, tooMany, Guids));
    }

    [Fact]
    public void Una_actualizacion_invalida_no_cambia_lo_ya_guardado()
    {
        var preference = NewPreference();
        preference.Update(false, new[] { "Gluten" }, Guids);

        Should.Throw<ArgumentException>(() => preference.Update(true, new[] { "Lactosa", " " }, Guids));

        preference.ShowAlerts.ShouldBeFalse();
        preference.WatchedItems.Single().Name.ShouldBe("Gluten");
    }

    [Fact]
    public void Actualiza_por_diferencia_y_conserva_las_entradas_que_siguen()
    {
        var preference = NewPreference();
        preference.Update(true, new[] { "Gluten", "Maní" }, Guids);
        var glutenId = preference.WatchedItems.Single(x => x.Name == "Gluten").Id;

        preference.Update(true, new[] { "gluten", "Lactosa" }, Guids);

        preference.WatchedItems.Select(x => x.Name).OrderBy(x => x).ToArray()
            .ShouldBe(new[] { "Gluten", "Lactosa" });
        preference.WatchedItems.Single(x => x.Name == "Gluten").Id.ShouldBe(glutenId);
    }

    [Fact]
    public void Una_lista_vacia_borra_todas_las_entradas()
    {
        var preference = NewPreference();
        preference.Update(true, new[] { "Gluten" }, Guids);
        preference.Update(true, Array.Empty<string>(), Guids);
        preference.WatchedItems.ShouldBeEmpty();
    }
}