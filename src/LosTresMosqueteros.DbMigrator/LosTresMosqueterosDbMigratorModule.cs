using LosTresMosqueteros.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace LosTresMosqueteros.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(LosTresMosqueterosEntityFrameworkCoreModule),
    typeof(LosTresMosqueterosApplicationContractsModule)
)]
public class LosTresMosqueterosDbMigratorModule : AbpModule
{
}
