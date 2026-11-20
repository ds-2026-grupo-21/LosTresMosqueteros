using System.Reflection;
using System.Threading.Tasks;
using LosTresMosqueteros.EntityFrameworkCore;
using LosTresMosqueteros.Permissions;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace LosTresMosqueteros.Productos;

public class ProductAdministrationTests : LosTresMosqueterosApplicationTestBase<LosTresMosqueterosEntityFrameworkCoreTestModule>
{
    [Theory]
    [InlineData(LosTresMosqueterosPermissions.Administration.Products)]
    [InlineData(LosTresMosqueterosPermissions.Administration.Audit)]
    [InlineData(LosTresMosqueterosPermissions.Administration.IntegrationMetrics)]
    public async Task Los_permisos_administrativos_estan_registrados(string name)
    {
        var definitions = GetRequiredService<IPermissionDefinitionManager>();

        (await definitions.GetOrNullAsync(name)).ShouldNotBeNull();
    }

    [Fact]
    public void El_crud_de_productos_internos_requiere_el_permiso_de_productos()
    {
        var authorization = typeof(ProductoAppService).GetCustomAttribute<AuthorizeAttribute>();

        authorization.ShouldNotBeNull();
        authorization.Policy.ShouldBe(LosTresMosqueterosPermissions.Administration.Products);
    }
}
