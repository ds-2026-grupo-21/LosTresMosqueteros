using System.Threading.Tasks;

namespace LosTresMosqueteros.Data;

public interface ILosTresMosqueterosDbSchemaMigrator
{
    Task MigrateAsync();
}
