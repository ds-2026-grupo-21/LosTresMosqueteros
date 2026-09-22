using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Microsoft.AspNetCore.Authorization;

namespace LosTresMosqueteros.Productos;

[AllowAnonymous]
public class ProductoAppService : ApplicationService, IProductoAppService
{
    private readonly IRepository<Producto, Guid> _productoRepository;

    public ProductoAppService(IRepository<Producto, Guid> productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task<ProductoDto> CreateAsync(CreateProductoDto input)
    {
        var producto = new Producto(
            GuidGenerator.Create(),
            input.CodigoBarras,
            input.Nombre,
            input.Ingredientes,
            input.Alergenos
        );

        await _productoRepository.InsertAsync(producto);

        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }

    public async Task<ProductoDto> GetAsync(Guid id)
    {
        var producto = await _productoRepository.GetAsync(id);

        return ObjectMapper.Map<Producto, ProductoDto>(producto);
    }
}