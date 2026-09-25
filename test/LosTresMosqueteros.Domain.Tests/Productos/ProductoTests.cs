using LosTresMosqueteros.Productos;
using Shouldly;
using System;
using Volo.Abp.Validation;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class ProductoTests
{
    [Fact]
    public void Deberia_Crear_Producto_Valido_Y_Normalizar_Datos()
    {
        // Arrange & Act
        var producto = new Producto(
            Guid.NewGuid(),
            "  7791234567890  ",
            "  Leche entera 1L  ",
            "Leche entera pasteurizada",
            "Contiene lactosa"
        );

        // Assert
        producto.CodigoBarras.ShouldBe("7791234567890");
        producto.Nombre.ShouldBe("Leche entera 1L");
        producto.Ingredientes.ShouldBe("Leche entera pasteurizada");
        producto.Alergenos.ShouldBe("Contiene lactosa");
    }

    [Fact]
    public void Deberia_Rechazar_Nombre_Vacio_O_Con_Solo_Espacios()
    {
        Should.Throw<ArgumentException>(() =>
        {
            new Producto(
                Guid.NewGuid(),
                "7791234567890",
                "   "
            );
        });
    }

    [Fact]
    public void Deberia_Rechazar_CodigoBarras_Vacio()
    {
        Should.Throw<ArgumentException>(() =>
        {
            new Producto(
                Guid.NewGuid(),
                "",
                "Leche entera 1L"
            );
        });
    }

    [Fact]
    public void Modificacion_valida_normaliza_datos()
    {
        var producto = new Producto(Guid.NewGuid(), "7791234567890", "Yerba Original");

        producto.SetNombre("  Yerba Premium  ");

        producto.Nombre.ShouldBe("Yerba Premium");
    }

    [Fact]
    public void Modificacion_invalida_no_deja_estado_parcial()
    {
        var producto = new Producto(Guid.NewGuid(), "7791234567890", "Yerba Original");

        Should.Throw<ArgumentException>(() => producto.SetNombre("   "));

        producto.Nombre.ShouldBe("Yerba Original");
    }

}