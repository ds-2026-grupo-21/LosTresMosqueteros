using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class ConsultarProductoExternoDtoTests
{
    [Theory]
    [InlineData("3017620422003")]
    [InlineData("0000000000000")]
    public void Deberia_Aceptar_Codigo_De_Barras_Numerico(string codigo)
    {
        Validar(codigo).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("abcdefgh")]
    [InlineData("30176204220A3")]
    [InlineData("30176204 2003")]
    public void Deberia_Rechazar_Codigo_De_Barras_No_Numerico(string codigo)
    {
        Validar(codigo).ShouldContain(error =>
            error.MemberNames.Contains(nameof(ConsultarProductoExternoDto.CodigoBarras)));
    }

    private static List<ValidationResult> Validar(string codigo)
    {
        var entrada = new ConsultarProductoExternoDto { CodigoBarras = codigo };
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(entrada, new ValidationContext(entrada), errores,
            validateAllProperties: true);
        return errores;
    }
}
