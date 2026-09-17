using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LosTresMosqueteros.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class LosTresMosqueterosDbContextFactory : IDesignTimeDbContextFactory<LosTresMosqueterosDbContext>
{
    public LosTresMosqueterosDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        LosTresMosqueterosEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<LosTresMosqueterosDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new LosTresMosqueterosDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../LosTresMosqueteros.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
