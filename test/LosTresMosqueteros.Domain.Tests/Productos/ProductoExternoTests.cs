using System;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class ProductoExternoTests
{
    [Fact]
    public void Conserva_datos_ausentes_y_ceros_iniciales()
    {
        var p = Producto.DesdeCatalogo(Guid.NewGuid(), "0009800800049", null, " ", null, null, null, null, null, null);
        p.CodigoBarras.ShouldBe("0009800800049"); p.Nombre.ShouldBeNull(); p.Marca.ShouldBeNull();
        p.Ingredientes.ShouldBeNull(); p.Alergenos.ShouldBeNull(); p.NutriScore.ShouldBeNull(); p.GrupoNova.ShouldBeNull();
    }
    [Fact]
    public void Conserva_detalle_externo_sin_truncar_y_separa_validacion_manual()
    {
        var name = new string('a', 200);
        var p = Producto.DesdeCatalogo(Guid.NewGuid(), "3017620422003", name, "Marca", "https://example.com/i.png",
            "Categoría", new string('x', 2000), "en:milk", "e", 4);
        p.Nombre.ShouldBe(name); p.Ingredientes!.Length.ShouldBe(2000);
        p.NutriScore.ShouldBe("e"); p.GrupoNova.ShouldBe(4);
        Should.Throw<ArgumentException>(() => new Producto(Guid.NewGuid(), "3017620422003", " "));
    }
}
