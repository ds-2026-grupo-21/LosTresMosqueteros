using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace LosTresMosqueteros.Productos;

public interface IProductoRepository : IRepository<Producto, Guid>
{
    Task<Producto> InsertarUnicoAsync(Producto producto, bool devolverExistente);
    Task<Producto> ActualizarUnicoAsync(Guid id, string codigo, string nombre, string? ingredientes, string? alergenos);
}
