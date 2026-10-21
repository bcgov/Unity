using Unity.GrantManager.Localization;
using Unity.Modules.Shared.Features;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Permissions;

public class MetabasePermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var metabasePermissionsGroup = context.AddGroup(MetabasePermissions.GroupName, L("Permission:Metabase"));

        // Granted on the tenant's Roles and Users screens; only tenants with Reporting use Metabase.
        metabasePermissionsGroup.AddPermission(MetabasePermissions.Access, L("Permission:Metabase.Access"), MultiTenancySides.Tenant)
            .RequireFeatures(FeatureConsts.Reporting);
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<GrantManagerResource>(name);
    }
}
