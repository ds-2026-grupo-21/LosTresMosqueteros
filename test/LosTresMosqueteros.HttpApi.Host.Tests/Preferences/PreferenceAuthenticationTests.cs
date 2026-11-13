using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LosTresMosqueteros.Preferences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros;

public class PreferenceAuthenticationTests
{
    [Fact]
    public void El_AppService_de_preferencias_exige_autenticacion_y_no_permite_anonimos()
    {
        typeof(PreferenceAppService).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
        typeof(PreferenceAppService).GetCustomAttributes(typeof(AllowAnonymousAttribute), true).ShouldBeEmpty();
    }

    [Fact]
    public async Task Rutas_de_prueba_protegidas_con_cookie_devuelven_401_sin_sesion()
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
        app.MapGet("/api/app/preference", () => "api").RequireAuthorization();
        app.MapPut("/api/app/preference", () => "api").RequireAuthorization();
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(address)
        };

        (await client.GetAsync("/api/app/preference")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsync("/api/app/preference", new StringContent("{}"))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
