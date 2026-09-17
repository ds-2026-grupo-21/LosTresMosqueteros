using Volo.Abp.Modularity;

namespace LosTresMosqueteros;

[DependsOn(
    typeof(LosTresMosqueterosApplicationModule),
    typeof(LosTresMosqueterosDomainTestModule)
)]
public class LosTresMosqueterosApplicationTestModule : AbpModule
{

}
