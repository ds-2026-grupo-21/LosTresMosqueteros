using LosTresMosqueteros.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace LosTresMosqueteros.Permissions;

public class LosTresMosqueterosPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(LosTresMosqueterosPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(LosTresMosqueterosPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<LosTresMosqueterosResource>(name);
    }
}
