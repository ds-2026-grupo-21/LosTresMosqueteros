using System;
using System.Threading.Tasks;
using LosTresMosqueteros.Productos;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class ProductoUnicoTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    [Fact]
    public async Task Repetir_guardado_devuelve_mismo_id_y_una_sola_fila()
    {
        var repo = GetRequiredService<IProductoRepository>();
        var code = "0000000000123";
        var first = await repo.InsertarUnicoAsync(new Producto(Guid.NewGuid(), code, "Uno"), true);
        var second = await repo.InsertarUnicoAsync(new Producto(Guid.NewGuid(), code, "Otro"), true);
        second.Id.ShouldBe(first.Id);
        (await WithUnitOfWorkAsync(() => repo.GetListAsync(x => x.CodigoBarras == code))).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Alta_manual_y_edicion_no_pueden_duplicar_codigo()
    {
        var service = GetRequiredService<IProductoAppService>();
        var first = await service.CreateAsync(new() { CodigoBarras = "0000000000111", Nombre = "Uno" });
        var second = await service.CreateAsync(new() { CodigoBarras = "0000000000222", Nombre = "Dos" });
        await Should.ThrowAsync<UserFriendlyException>(() => service.CreateAsync(new() {
            CodigoBarras = "0000000000111", Nombre = "Duplicado" }));
        await Should.ThrowAsync<UserFriendlyException>(() => service.UpdateAsync(second.Id, new() {
            CodigoBarras = "0000000000111", Nombre = "Cambio rechazado" }));
        var intact = await service.GetAsync(second.Id);
        intact.CodigoBarras.ShouldBe("0000000000222"); intact.Nombre.ShouldBe("Dos");
    }

    [Fact]
    public async Task Recupera_producto_externo_sin_nombre_y_sin_recortar_datos()
    {
        var repo = GetRequiredService<IProductoRepository>();
        var text = new string('a', 2000);
        var entity = Producto.DesdeCatalogo(Guid.NewGuid(), "0000000000333", null, "Marca", null,
            "Categoría", text, "en:milk, en:nuts", "b", 2);
        await repo.InsertarUnicoAsync(entity, true);
        var result = await GetRequiredService<IProductoAppService>().GetAsync(entity.Id);
        result.Nombre.ShouldBeNull(); result.Ingredientes.ShouldBe(text);
        result.Marca.ShouldBe("Marca"); result.NutriScore.ShouldBe("b"); result.GrupoNova.ShouldBe(2);
    }
}
