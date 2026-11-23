using Unity.GrantManager.Localization;
using Unity.Reporting.Localization;
using Unity.Reporting.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.GrantManager.Permissions;

public class GrantManagerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var grantManagerPermissionsGroup = context.AddGroup(GrantManagerPermissions.GroupName, L("Permission:GrantManagerManagement"));

        // Default grant manager user
        grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.Default, L("Permission:GrantManagerManagement.Default"));

        var organizationPermissions = grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.Organizations.Default, L("Permission:GrantApplicationManagement.Organizations.Default"));
        organizationPermissions.AddChild(GrantManagerPermissions.Organizations.ManageProfiles, L("Permission:GrantApplicationManagement.Organizations.ManageProfiles"));

        grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.Intakes.Default, L("Permission:GrantManagerManagement.Intakes.Default"));

        var applicationFormPermissions = grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.ApplicationForms.Default, L("Permission:GrantManagerManagement.ApplicationForms.Default"));
        applicationFormPermissions.AddChild(GrantManagerPermissions.ApplicationForms.Delete, L("Permission:GrantManagerManagement.ApplicationForms.Delete"));

        var notificationSchedulerPermissions = grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.NotificationScheduler.Default, L("Permission:GrantManagerManagement.NotificationScheduler.Default"));
        notificationSchedulerPermissions.AddChild(GrantManagerPermissions.NotificationScheduler.ManageSchedules, L("Permission:GrantManagerManagement.NotificationScheduler.ManageSchedules"));
    }

    public override void PostDefine(IPermissionDefinitionContext context)
    {
        // Move the reporting branch after all modules have defined their permissions, preserving existing grants.
        context.RemoveGroup(ReportingPermissions.GroupName);
        var applicationFormPermissions = context.GetPermissionOrNull(GrantManagerPermissions.ApplicationForms.Default)!;
        var reportingPermissions = applicationFormPermissions.AddChild(
            ReportingPermissions.Configuration.Default,
            LocalizableString.Create<ReportingResource>("Permission:Reporting.Configuration.Default"));

        reportingPermissions.AddChild(ReportingPermissions.Configuration.Update,
            LocalizableString.Create<ReportingResource>("Permission:Reporting.Configuration.Update"));
        reportingPermissions.AddChild(ReportingPermissions.Configuration.Delete,
            LocalizableString.Create<ReportingResource>("Permission:Reporting.Configuration.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<GrantManagerResource>(name);
    }
}
