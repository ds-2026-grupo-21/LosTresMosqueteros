using LosTresMosqueteros.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace LosTresMosqueteros.Permissions;

public class LosTresMosqueterosPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var administration = context.AddGroup(
            LosTresMosqueterosPermissions.GroupName, L("Permission:Administration"));

        administration.AddPermission(
            LosTresMosqueterosPermissions.Administration.Products, L("Permission:InternalProducts"));
        administration.AddPermission(
            LosTresMosqueterosPermissions.Administration.Audit, L("Permission:Audit"));
        administration.AddPermission(
            LosTresMosqueterosPermissions.Administration.IntegrationMetrics, L("Permission:IntegrationMetrics"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LosTresMosqueterosResource>(name);
    }
}
