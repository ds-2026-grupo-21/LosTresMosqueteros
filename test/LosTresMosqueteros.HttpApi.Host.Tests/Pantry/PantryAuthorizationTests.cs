using System.Linq;
using System.Reflection;
using LosTresMosqueteros.Pantry;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using Xunit;

namespace LosTresMosqueteros;

public class PantryAuthorizationTests
{
    [Fact]
    public void El_AppService_de_despensa_exige_autenticacion_en_todos_sus_metodos()
    {
        typeof(PantryAppService).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
        typeof(PantryAppService).GetCustomAttributes(typeof(AllowAnonymousAttribute), true).ShouldBeEmpty();

        typeof(PantryAppService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any())
            .ShouldBeEmpty();
    }

    [Fact]
    public void El_worker_y_su_manager_funcionan_sin_sesion()
    {
        typeof(ExpirationWarningWorker).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldBeEmpty();
        typeof(ExpirationWarningManager).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldBeEmpty();
    }
}