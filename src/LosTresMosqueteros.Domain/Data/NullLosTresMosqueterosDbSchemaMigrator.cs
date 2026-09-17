using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace LosTresMosqueteros.Data;

/* This is used if database provider does't define
 * ILosTresMosqueterosDbSchemaMigrator implementation.
 */
public class NullLosTresMosqueterosDbSchemaMigrator : ILosTresMosqueterosDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
