using Unity.GrantManager.Localization;
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

        grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.ApplicationForms.Default, L("Permission:GrantManagerManagement.ApplicationForms.Default"));

        var applicantPortalPermissions = grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.ApplicantPortal.Default, L("Permission:GrantManagerManagement.ApplicantPortal.Default"));
        applicantPortalPermissions.AddChild(GrantManagerPermissions.ApplicantPortal.EditProgramDetails, L("Permission:GrantManagerManagement.ApplicantPortal.EditProgramDetails"));

        var notificationSchedulerPermissions = grantManagerPermissionsGroup.AddPermission(GrantManagerPermissions.NotificationScheduler.Default, L("Permission:GrantManagerManagement.NotificationScheduler.Default"));
        notificationSchedulerPermissions.AddChild(GrantManagerPermissions.NotificationScheduler.ManageSchedules, L("Permission:GrantManagerManagement.NotificationScheduler.ManageSchedules"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<GrantManagerResource>(name);
    }
}
