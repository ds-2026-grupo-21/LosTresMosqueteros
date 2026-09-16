using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LosTresMosqueteros.Data;
using Volo.Abp.DependencyInjection;

namespace LosTresMosqueteros.EntityFrameworkCore;

public class EntityFrameworkCoreLosTresMosqueterosDbSchemaMigrator
    : ILosTresMosqueterosDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreLosTresMosqueterosDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the LosTresMosqueterosDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<LosTresMosqueterosDbContext>()
            .Database
            .MigrateAsync();
    }
}
