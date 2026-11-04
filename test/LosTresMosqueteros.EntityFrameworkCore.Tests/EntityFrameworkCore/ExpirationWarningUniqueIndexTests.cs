using System;
using System.Linq;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class ExpirationWarningUniqueIndexTests
    : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    private readonly IRepository<ExpirationWarning, Guid> _warnings;
    private readonly IRepository<PantryItem, Guid> _items;

    public ExpirationWarningUniqueIndexTests()
    {
        _warnings = GetRequiredService<IRepository<ExpirationWarning, Guid>>();
        _items = GetRequiredService<IRepository<PantryItem, Guid>>();
    }

    [Fact]
    public async Task Indice_Unico_Debe_Rechazar_Segunda_Advertencia_Del_Mismo_Item_Y_Tipo()
    {
        var itemId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 10, 15);

        await CreateItemAsync(itemId, fecha);

        await WithUnitOfWorkAsync(async () =>
        {
            await _warnings.InsertAsync(
                new ExpirationWarning(Guid.NewGuid(), itemId, fecha, 1),
                autoSave: true);
        });

        // Segunda inserción con la misma clave lógica (PantryItemId, Type): debe fallar
        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await WithUnitOfWorkAsync(async () =>
            {
                await _warnings.InsertAsync(
                    new ExpirationWarning(Guid.NewGuid(), itemId, fecha, 1),
                    autoSave: true);
            });
        });

        // La base conserva una sola advertencia para ese ítem
        var cantidad = await WithUnitOfWorkAsync(async () =>
            (await _warnings.GetListAsync(w => w.PantryItemId == itemId)).Count);

        cantidad.ShouldBe(1);
    }

    [Fact]
    public async Task Indice_Unico_No_Debe_Afectar_A_Items_Distintos()
    {
        var fecha = new DateOnly(2026, 10, 15);
        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();

        await CreateItemAsync(item1, fecha);
        await CreateItemAsync(item2, fecha);

        await WithUnitOfWorkAsync(async () =>
        {
            await _warnings.InsertAsync(new ExpirationWarning(Guid.NewGuid(), item1, fecha, 1), autoSave: true);
            await _warnings.InsertAsync(new ExpirationWarning(Guid.NewGuid(), item2, fecha, 1), autoSave: true);
        });

        var cantidad = await WithUnitOfWorkAsync(async () =>
            (await _warnings.GetListAsync(w => w.PantryItemId == item1 || w.PantryItemId == item2)).Count);

        cantidad.ShouldBe(2);
    }

    [Fact]
    public async Task No_Debe_Crear_Advertencia_Para_Un_Item_Eliminado()
    {
        var itemId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 10, 15);
        await CreateItemAsync(itemId, fecha);
        await WithUnitOfWorkAsync(() => _items.DeleteAsync(itemId, autoSave: true));

        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(() =>
            _warnings.InsertAsync(new ExpirationWarning(Guid.NewGuid(), itemId, fecha, 1), autoSave: true)));
    }

    [Fact]
    public async Task Eliminar_Item_Debe_Eliminar_Sus_Advertencias()
    {
        var itemId = Guid.NewGuid();
        var fecha = new DateOnly(2026, 10, 15);
        await CreateItemAsync(itemId, fecha);
        await WithUnitOfWorkAsync(() => _warnings.InsertAsync(
            new ExpirationWarning(Guid.NewGuid(), itemId, fecha, 1), autoSave: true));

        await WithUnitOfWorkAsync(() => _items.DeleteAsync(itemId, autoSave: true));

        (await WithUnitOfWorkAsync(() => _warnings.GetListAsync(x => x.PantryItemId == itemId))).ShouldBeEmpty();
    }

    private Task CreateItemAsync(Guid itemId, DateOnly fecha) => WithUnitOfWorkAsync(() =>
        _items.InsertAsync(new PantryItem(itemId, Guid.NewGuid(), Guid.NewGuid(), fecha), autoSave: true));
}
