using Unity.Reporting.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.Reporting.Permissions;

public class ReportingPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var reportingPermissionsGroup = context.AddGroup(ReportingPermissions.GroupName, L("Permission:Reporting"));
        
        // Reporting Permissions
        var reportingPermissions =
            reportingPermissionsGroup.AddPermission(ReportingPermissions.Configuration.Default, L("Permission:Reporting.Configuration.Default"));
        
        reportingPermissions.AddChild(ReportingPermissions.Configuration.Update, L("Permission:Reporting.Configuration.Update"));
        reportingPermissions.AddChild(ReportingPermissions.Configuration.Delete, L("Permission:Reporting.Configuration.Delete"));       
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ReportingResource>(name);
    }
}
