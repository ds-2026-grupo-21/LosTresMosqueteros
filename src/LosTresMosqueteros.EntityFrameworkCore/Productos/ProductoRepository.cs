using System;
using System.Threading.Tasks;
using LosTresMosqueteros.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.Uow;

namespace LosTresMosqueteros.Productos;

public class ProductoRepository : EfCoreRepository<LosTresMosqueterosDbContext, Producto, Guid>, IProductoRepository
{
    private readonly IUnitOfWorkManager _units;
    public ProductoRepository(IDbContextProvider<LosTresMosqueterosDbContext> provider, IUnitOfWorkManager units)
        : base(provider) { _units = units; }

    public async Task<Producto> InsertarUnicoAsync(Producto producto, bool devolverExistente)
    {
        try
        {
            using var unit = _units.Begin(requiresNew: true, isTransactional: true);
            var existente = await FindAsync(x => x.CodigoBarras == producto.CodigoBarras);
            if (existente != null)
            {
                if (!devolverExistente) throw CodigoRepetido();
                return existente;
            }
            await InsertAsync(producto, autoSave: true);
            await unit.CompleteAsync();
            return producto;
        }
        catch (DbUpdateException)
        {
            // La unidad que falló ya se descartó. La consulta usa otra transacción y contexto.
            using var recovery = _units.Begin(requiresNew: true, isTransactional: false);
            var existente = await FindAsync(x => x.CodigoBarras == producto.CodigoBarras);
            if (existente == null) throw;
            if (!devolverExistente) throw CodigoRepetido();
            return existente;
        }
    }

    public async Task<Producto> ActualizarUnicoAsync(Guid id, string codigo, string nombre,
        string? ingredientes, string? alergenos)
    {
        // Validar todos los campos antes de modificar una entidad persistida.
        var validado = new Producto(id, codigo, nombre, ingredientes, alergenos);
        try
        {
            using var unit = _units.Begin(requiresNew: true, isTransactional: true);
            if (await (await GetDbSetAsync()).AnyAsync(x => x.Id != id && x.CodigoBarras == validado.CodigoBarras)) throw CodigoRepetido();
            var producto = await GetAsync(id);
            producto.SetCodigoBarras(validado.CodigoBarras);
            producto.SetNombre(validado.Nombre!);
            producto.SetIngredientes(ingredientes);
            producto.SetAlergenos(alergenos);
            await UpdateAsync(producto, autoSave: true);
            await unit.CompleteAsync();
            return producto;
        }
        catch (DbUpdateException)
        {
            using var recovery = _units.Begin(requiresNew: true, isTransactional: false);
            if (await (await GetDbSetAsync()).AnyAsync(x => x.Id != id && x.CodigoBarras == validado.CodigoBarras)) throw CodigoRepetido();
            throw;
        }
    }

    private static UserFriendlyException CodigoRepetido() => new("Ya existe un producto con ese código de barras.");
}
