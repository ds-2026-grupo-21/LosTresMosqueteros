using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LosTresMosqueteros.Productos;

[AllowAnonymous]
public class ProductoAppService : CrudAppService<Producto, ProductoDto, ProductoDto, Guid, PagedAndSortedResultRequestDto, CreateProductoDto, CreateProductoDto>, IProductoAppService
{
    public ProductoAppService(IRepository<Producto, Guid> repository)
        : base(repository)
    {
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

        await Repository.InsertAsync(producto);

        return await MapToGetOutputDtoAsync(producto);
    }

    public override async Task<ProductoDto> UpdateAsync(Guid id, CreateProductoDto input)
    {
        var producto = await Repository.GetAsync(id);

        producto.SetCodigoBarras(input.CodigoBarras);
        producto.SetNombre(input.Nombre);
        producto.SetIngredientes(input.Ingredientes);
        producto.SetAlergenos(input.Alergenos);

        await Repository.UpdateAsync(producto);

        return await MapToGetOutputDtoAsync(producto);
    }

    public override Task DeleteAsync(Guid id)
    {
        throw new UserFriendlyException(
            "No se permite eliminar productos: la operacion no es coherente con este modelo."
        );
    }
}
