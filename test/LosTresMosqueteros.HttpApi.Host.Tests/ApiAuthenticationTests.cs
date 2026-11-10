using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros;

public class ApiAuthenticationTests
{
    [Fact]
    public async Task Una_peticion_sin_sesion_a_la_api_devuelve_401_y_la_web_conserva_la_redireccion()
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
        app.MapGet("/api/app/pantry", () => "api").RequireAuthorization();
        app.MapGet("/pagina", () => "web").RequireAuthorization();
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(address)
        };
        (await client.GetAsync("/api/app/pantry")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var web = await client.GetAsync("/pagina");
        web.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        web.Headers.Location!.AbsolutePath.ShouldBe("/Account/Login");
    }
}
