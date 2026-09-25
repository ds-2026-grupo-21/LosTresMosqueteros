using LosTresMosqueteros.Productos;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace LosTresMosqueteros;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class LosTresMosqueterosApplicationMappers : MapperBase<Producto, ProductoDto>
{
    public override partial ProductoDto Map(Producto source);

    public override partial void Map(Producto source, ProductoDto destination);
}