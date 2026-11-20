using System;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Pantry;

public class ExpirationRuleTests
{
    private static readonly DateOnly Today = new(2026, 10, 21);

    [Theory]
    [InlineData(-30, ExpirationState.Expired)]
    [InlineData(-4, ExpirationState.Expired)]
    [InlineData(-1, ExpirationState.Expired)]
    [InlineData(0, ExpirationState.DueToday)]
    [InlineData(1, ExpirationState.DueSoon)]
    [InlineData(2, ExpirationState.DueSoon)]
    [InlineData(3, ExpirationState.DueSoon)]
    [InlineData(4, ExpirationState.None)]
    [InlineData(30, ExpirationState.None)]
    public void Clasifica_segun_los_dias_hasta_el_vencimiento(int days, ExpirationState expected)
    {
        ExpirationRule.GetState(PantryItemStatus.Active, Today.AddDays(days), Today).ShouldBe(expected);
    }

    [Fact]
    public void Un_item_sin_fecha_no_tiene_aviso()
    {
        ExpirationRule.GetState(PantryItemStatus.Active, null, Today).ShouldBe(ExpirationState.None);
        ExpirationRule.DaysRemaining(null, Today).ShouldBeNull();
    }

    [Theory]
    [InlineData(-4)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Un_item_consumido_no_tiene_aviso_aunque_este_en_el_umbral(int days)
    {
        ExpirationRule.GetState(PantryItemStatus.Consumed, Today.AddDays(days), Today)
            .ShouldBe(ExpirationState.None);
    }

    [Theory]
    [InlineData(-4, true)]
    [InlineData(0, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void Debe_advertir_solo_dentro_del_umbral(int days, bool expected)
    {
        ExpirationRule.ShouldWarn(PantryItemStatus.Active, Today.AddDays(days), Today).ShouldBe(expected);
    }

    [Fact]
    public void Los_dias_restantes_son_negativos_si_ya_vencio()
    {
        ExpirationRule.DaysRemaining(Today.AddDays(-4), Today).ShouldBe(-4);
        ExpirationRule.DaysRemaining(Today, Today).ShouldBe(0);
        ExpirationRule.DaysRemaining(Today.AddDays(3), Today).ShouldBe(3);
    }

    [Fact]
    public void El_umbral_cruza_el_fin_de_mes_y_de_anio()
    {
        ExpirationRule.GetState(PantryItemStatus.Active, new DateOnly(2026, 11, 2), new DateOnly(2026, 10, 30))
            .ShouldBe(ExpirationState.DueSoon);
        ExpirationRule.GetState(PantryItemStatus.Active, new DateOnly(2027, 1, 2), new DateOnly(2026, 12, 30))
            .ShouldBe(ExpirationState.DueSoon);
        ExpirationRule.GetState(PantryItemStatus.Active, new DateOnly(2027, 1, 3), new DateOnly(2026, 12, 30))
            .ShouldBe(ExpirationState.None);
    }

    [Fact]
    public void El_umbral_es_de_tres_dias()
    {
        ExpirationRule.ThresholdDays.ShouldBe(3);
    }
}