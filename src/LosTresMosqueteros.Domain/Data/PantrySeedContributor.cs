using System;
using System.Linq;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Productos;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Timing;

namespace LosTresMosqueteros.Data;

/// <summary>
/// Datos locales de ejemplo para el worker de vencimientos (TP08).
/// El propietario es un Guid fijo: no hay API pública que permita elegirlo.
/// </summary>
public class PantrySeedContributor : IDataSeedContributor, ITransientDependency
{
    public static readonly Guid SeedOwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IRepository<PantryItem, Guid> _items;
    private readonly IRepository<Producto, Guid> _productos;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IClock _clock;

    public PantrySeedContributor(
        IRepository<PantryItem, Guid> items,
        IRepository<Producto, Guid> productos,
        IGuidGenerator guidGenerator,
        IClock clock)
    {
        _items = items;
        _productos = productos;
        _guidGenerator = guidGenerator;
        _clock = clock;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // Idempotente: si ya hay ítems del propietario de prueba, no se repite
        if (await _items.AnyAsync(x => x.OwnerId == SeedOwnerId))
        {
            return;
        }

        // Se usa un producto existente; si no hay ninguno, no se siembra nada
        var producto = (await _productos.GetListAsync()).FirstOrDefault();
        if (producto is null)
        {
            return;
        }

        // Criterio: fecha UTC, igual que el worker y las pruebas
        var today = DateOnly.FromDateTime(_clock.Now.ToUniversalTime());

        var fechas = new DateOnly?[]
        {
            today.AddDays(-4),  // vencido
            today,              // vence hoy
            today.AddDays(3),   // día límite del umbral
            today.AddDays(10),  // fuera del umbral
            null                // sin fecha
        };

        foreach (var fecha in fechas)
        {
            await _items.InsertAsync(
                new PantryItem(_guidGenerator.Create(), SeedOwnerId, producto.Id, fecha));
        }

        // Consumido, con fecha dentro del umbral: no debe generar advertencia
        var consumido = new PantryItem(_guidGenerator.Create(), SeedOwnerId, producto.Id, today.AddDays(1));
        consumido.MarkConsumed();
        await _items.InsertAsync(consumido);
    }
}