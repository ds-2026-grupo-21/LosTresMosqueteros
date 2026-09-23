using System;
using System.Threading.Tasks;

namespace LosTresMosqueteros.Productos;

public interface IProductoAppService
{
    Task<ProductoDto> CreateAsync(CreateProductoDto input);

    Task<ProductoDto> GetAsync(Guid id);
}