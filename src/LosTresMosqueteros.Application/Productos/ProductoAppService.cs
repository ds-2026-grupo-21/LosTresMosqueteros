using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LosTresMosqueteros.Productos;

[Authorize]
public class ProductoAppService : CrudAppService<Producto, ProductoDto, ProductoDto, Guid, PagedAndSortedResultRequestDto, CreateProductoDto, CreateProductoDto>, IProductoAppService
{
    private readonly IProductoRepository _productos;
    public ProductoAppService(IProductoRepository repository)
        : base(repository)
    {
        _productos = repository;
    }

    public override async Task<ProductoDto> CreateAsync(CreateProductoDto input)
    {
        var producto = new Producto(
            GuidGenerator.Create(),
            input.CodigoBarras,
            input.Nombre,
            input.Ingredientes,
            input.Alergenos
        );

        producto = await _productos.InsertarUnicoAsync(producto, devolverExistente: false);

        return await MapToGetOutputDtoAsync(producto);
    }

    public override async Task<ProductoDto> UpdateAsync(Guid id, CreateProductoDto input)
    {
        var producto = await _productos.ActualizarUnicoAsync(id, input.CodigoBarras, input.Nombre,
            input.Ingredientes, input.Alergenos);

        return await MapToGetOutputDtoAsync(producto);
    }

    public override Task DeleteAsync(Guid id)
    {
        throw new UserFriendlyException(
            "No se permite eliminar productos: la operacion no es coherente con este modelo."
        );
    }
}
