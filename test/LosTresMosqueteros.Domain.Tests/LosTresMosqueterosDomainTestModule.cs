using Volo.Abp.Modularity;

namespace LosTresMosqueteros;

[DependsOn(
    typeof(LosTresMosqueterosDomainModule),
    typeof(LosTresMosqueterosTestBaseModule)
)]
public class LosTresMosqueterosDomainTestModule : AbpModule
{

}
