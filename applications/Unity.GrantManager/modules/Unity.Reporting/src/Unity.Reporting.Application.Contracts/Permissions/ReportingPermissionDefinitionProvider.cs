using Unity.Reporting.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Unity.Reporting.Permissions;

/// <summary>
/// ABP Framework permission definition provider for the Unity.Reporting module.
/// Registers all reporting-related permissions with the authorization system including
/// configuration management permissions with proper localization and hierarchical organization.
/// </summary>
public class ReportingPermissionDefinitionProvider : PermissionDefinitionProvider
{
    /// <summary>
    /// Defines all Unity.Reporting module permissions and registers them with the ABP permission system.
    /// Creates the main reporting permission group and adds configuration-related permissions with
    /// proper parent-child relationships and localized display names for administrative interfaces.
    /// </summary>
    /// <param name="context">The permission definition context for registering permission groups and individual permissions.</param>
    public override void Define(IPermissionDefinitionContext context)
    {
        // Create the main reporting permissions group with localized display name
        var reportingPermissionsGroup = context.AddGroup(ReportingPermissions.GroupName, L("Permission:Reporting"));
        
        // Add the base reporting configuration permission
        var reportingPermissions =
            reportingPermissionsGroup.AddPermission(ReportingPermissions.Configuration.Default, L("Permission:Reporting.Configuration.Default"));
        
        // Add child permissions for specific configuration operations
        reportingPermissions.AddChild(ReportingPermissions.Configuration.Update, L("Permission:Reporting.Configuration.Update"));
        reportingPermissions.AddChild(ReportingPermissions.Configuration.Delete, L("Permission:Reporting.Configuration.Delete"));       
    }

    /// <summary>
    /// Creates a localizable string using the Unity.Reporting localization resource.
    /// Provides a convenient method for creating localized permission display names
    /// that will be resolved at runtime based on the current user's language settings.
    /// </summary>
    /// <param name="name">The localization key for the permission display name.</param>
    /// <returns>A localizable string that will be resolved using the ReportingResource localization provider.</returns>
    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<ReportingResource>(name);
    }
}
