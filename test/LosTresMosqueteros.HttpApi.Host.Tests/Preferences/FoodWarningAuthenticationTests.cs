using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LosTresMosqueteros.Pantry;
using LosTresMosqueteros.Preferences;
using LosTresMosqueteros.Productos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Application.Services;
using Xunit;

namespace LosTresMosqueteros;

public class FoodWarningAuthenticationTests
{
    [Theory]
    [InlineData(typeof(ConsultaProductoExternoAppService))]
    [InlineData(typeof(GuardarProductoExternoAppService))]
    [InlineData(typeof(PantryAppService))]
    public void Los_servicios_que_devuelven_advertencias_exigen_autenticacion(Type service)
    {
        service.GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
        service.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).ShouldBeEmpty();
    }

    [Fact]
    public void El_evaluador_no_es_un_servicio_de_aplicacion_y_no_se_expone_por_HTTP()
    {
        // Solo los IApplicationService se publican como endpoints; el evaluador es interno.
        typeof(IApplicationService).IsAssignableFrom(typeof(FoodWarningEvaluator)).ShouldBeFalse();
    }

    [Fact]
    public async Task Rutas_de_advertencias_protegidas_con_cookie_devuelven_401_sin_sesion()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme);
        builder.Services.AddAuthorization();
        LosTresMosqueterosHttpApiHostModule.ConfigureApiCookieResponses(builder.Services);

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/api/app/consulta-producto-externo", () => "api").RequireAuthorization();
        app.MapPost("/api/app/guardar-producto-externo", () => "api").RequireAuthorization();
        app.MapPost("/api/app/pantry", () => "api").RequireAuthorization();
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(address)
        };

        (await client.GetAsync("/api/app/consulta-producto-externo")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/api/app/guardar-producto-externo", new StringContent("{}"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/api/app/pantry", new StringContent("{}"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}