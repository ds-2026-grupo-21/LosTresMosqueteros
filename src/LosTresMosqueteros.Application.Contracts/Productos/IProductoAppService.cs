using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LosTresMosqueteros.Productos;

public interface IProductoAppService :
    ICrudAppService <
        ProductoDto,
        ProductoDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateProductoDto,
        CreateProductoDto>
{
}