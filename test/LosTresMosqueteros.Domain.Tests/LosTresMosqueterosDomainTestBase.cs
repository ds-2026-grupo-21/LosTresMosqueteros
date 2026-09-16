using Volo.Abp.Modularity;

namespace LosTresMosqueteros;

/* Inherit from this class for your domain layer tests. */
public abstract class LosTresMosqueterosDomainTestBase<TStartupModule> : LosTresMosqueterosTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
