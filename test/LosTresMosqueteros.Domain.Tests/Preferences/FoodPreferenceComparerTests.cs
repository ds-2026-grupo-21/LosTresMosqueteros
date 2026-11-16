using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Preferences;

public class FoodPreferenceComparerTests
{
    [Fact]
    public void Encuentra_una_coincidencia_en_los_alergenos()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Gluten" }, "Arroz, sal", "en:milk,en:gluten");

        result.HasPreferences.ShouldBeTrue();
        result.Matches.ShouldBe(new[] { new FoodMatch("Gluten", FoodField.Allergens) });
        result.IsComplete.ShouldBeTrue();
    }

    [Fact]
    public void Encuentra_una_coincidencia_en_los_ingredientes()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Leche" }, "Harina de trigo, azúcar, leche en polvo", "en:gluten");

        result.Matches.ShouldBe(new[] { new FoodMatch("Leche", FoodField.Ingredients) });
    }

    [Fact]
    public void Una_preferencia_presente_en_ambos_campos_genera_dos_coincidencias()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Milk" }, "Milk powder", "en:milk");

        result.Matches.Select(x => x.Field).ShouldBe(new[] { FoodField.Ingredients, FoodField.Allergens });
    }

    [Fact]
    public void Sin_coincidencias_sobre_datos_completos_no_hay_coincidencias_ni_limitaciones()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Maní" }, "Arroz, sal", "en:milk");

        result.HasPreferences.ShouldBeTrue();
        result.HasMatches.ShouldBeFalse();
        result.IsComplete.ShouldBeTrue();
    }

    [Fact]
    public void Sin_preferencias_informa_que_no_hay_nada_para_comparar()
    {
        var empty = FoodPreferenceComparer.Compare(Array.Empty<string>(), "Leche", "en:milk");
        var nullList = FoodPreferenceComparer.Compare(null, "Leche", "en:milk");

        empty.HasPreferences.ShouldBeFalse();
        empty.HasMatches.ShouldBeFalse();
        nullList.HasPreferences.ShouldBeFalse();
    }

    [Theory]
    [InlineData(" MANÍ ", "maní tostado")]
    [InlineData("Frutos-secos", "Frutos secos (nueces)")]
    [InlineData("tree nuts", "en:tree-nuts")]
    [InlineData("leche", "Contiene:LECHE.")]
    public void Ignora_diferencias_de_formato(string preference, string productText)
    {
        FoodPreferenceComparer.Compare(new[] { preference }, productText, null)
            .HasMatches.ShouldBeTrue();
    }

    [Theory]
    [InlineData("mani", "Manitas de cerdo")]
    [InlineData("sal", "Salsa de tomate")]
    [InlineData("soja", "Sojaleche")]
    public void No_coincide_por_fragmentos_accidentales(string preference, string productText)
    {
        FoodPreferenceComparer.Compare(new[] { preference }, productText, "en:milk")
            .HasMatches.ShouldBeFalse();
    }

    [Fact]
    public void No_traduce_ni_inventa_equivalencias()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Maní" }, "Peanuts", "en:peanuts");

        result.HasMatches.ShouldBeFalse();
    }

    [Fact]
    public void Los_datos_ausentes_son_no_verificables_y_no_ausencia_de_riesgo()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Maní" }, null, "   ");

        result.HasMatches.ShouldBeFalse();
        result.IsComplete.ShouldBeFalse();
        result.Unverifiable.ShouldBe(new[] { FoodField.Ingredients, FoodField.Allergens });
    }

    [Fact]
    public void Una_coincidencia_junto_con_informacion_incompleta_informa_ambas_cosas()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Milk" }, null, "en:milk");

        result.HasMatches.ShouldBeTrue();
        result.Unverifiable.ShouldBe(new[] { FoodField.Ingredients });
    }

    [Fact]
    public void Un_texto_que_no_se_puede_interpretar_es_no_verificable()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Leche" }, "!!!", "en:gluten");

        result.Unverifiable.ShouldBe(new[] { FoodField.Ingredients });
    }

    [Fact]
    public void Conserva_letras_unicode_y_no_compara_fragmentos()
    {
        var chinese = FoodPreferenceComparer.Compare(new[] { "大豆" }, "含有大豆", "zh:大豆");
        chinese.HasPreferences.ShouldBeTrue();
        chinese.Matches.ShouldContain(new FoodMatch("大豆", FoodField.Allergens));
        FoodPreferenceComparer.Compare(new[] { "œuf" }, "uf", "en:milk").HasMatches.ShouldBeFalse();
    }

    [Fact]
    public void Preferencia_no_interpretable_no_se_presenta_como_ausente_o_verificada()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "!!!" }, "Leche", "en:milk");
        result.HasPreferences.ShouldBeTrue();
        result.Unverifiable.ShouldBe(new[] { FoodField.Ingredients, FoodField.Allergens });
    }

    [Fact]
    public void Las_preferencias_repetidas_o_vacias_no_duplican_resultados()
    {
        var result = FoodPreferenceComparer.Compare(new[] { "Maní", "mani", "  ", null }, "Maní", null);

        result.Matches.Count.ShouldBe(1);
    }
}
