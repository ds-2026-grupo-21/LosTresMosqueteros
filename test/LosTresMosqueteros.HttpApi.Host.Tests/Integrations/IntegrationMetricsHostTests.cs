using System.Linq;
using LosTresMosqueteros.Integrations;
using LosTresMosqueteros.Permissions;
using LosTresMosqueteros.Productos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros;

public class IntegrationMetricsHostTests
{
    [Fact]
    public void Las_metricas_exigen_el_permiso_administrativo()
    {
        var authorize = typeof(IntegrationMetricsAppService)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .ShouldHaveSingleItem();
        authorize.Policy.ShouldBe(LosTresMosqueterosPermissions.Administration.IntegrationMetrics);

        typeof(IntegrationMetricsAppService).GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
            .ShouldBeEmpty();
        typeof(IntegrationMetricsAppService).GetMethods()
            .Where(m => m.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any())
            .ShouldBeEmpty();
    }

    [Fact]
    public void Los_clientes_externos_quedan_envueltos_por_el_registro_de_metricas()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IExternalApiCallRecorder>());

        LosTresMosqueterosHttpApiHostModule.ConfigureExternalCatalogClients(services);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IExternalProductCatalogClient>()
            .ShouldBeOfType<MeteredExternalProductCatalogClient>();
        provider.GetRequiredService<IExternalProductSearchClient>()
            .ShouldBeOfType<MeteredExternalProductSearchClient>();
    }
}
