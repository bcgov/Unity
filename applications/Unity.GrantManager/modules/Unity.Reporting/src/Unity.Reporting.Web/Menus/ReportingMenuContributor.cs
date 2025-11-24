using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.UI.Navigation;

namespace Unity.Reporting.Web.Menus;

/// <summary>
/// ABP Framework menu contributor for the Unity.Reporting module navigation system.
/// Responsible for adding reporting-related menu items to the application's main navigation,
/// including administrative pages for reconciliation and reporting configuration management.
/// All menu items require IT Admin permissions for security and proper access control.
/// </summary>
public class ReportingMenuContributor : IMenuContributor
{
    /// <summary>
    /// Configures the application menu by adding Unity.Reporting module menu items.
    /// Delegates to the private method to set up reporting-specific navigation items
    /// with appropriate permissions and routing configuration.
    /// </summary>
    /// <param name="context">The menu configuration context containing the menu to be configured.</param>
    /// <returns>A task representing the asynchronous menu configuration operation.</returns>
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        await ConfigureReportingMenuAsync(context);
    }

    /// <summary>
    /// Configures reporting-specific menu items including reconciliation and reporting configuration pages.
    /// Adds navigation items with IT Admin permission requirements and proper routing to ensure
    /// administrative functionality is accessible only to authorized users with appropriate permissions.
    /// </summary>
    /// <param name="context">The menu configuration context for adding reporting menu items.</param>
    /// <returns>A completed task representing the synchronous menu item addition operations.</returns>
    private static Task ConfigureReportingMenuAsync(MenuConfigurationContext context)
    {
        // Add Reconciliation menu item for IT Admin users
        context.Menu.AddItem(
            new ApplicationMenuItem(
                    ReportingMenus.Prefix,
                    displayName: "Reconciliation",
                    "~/TenantManagement/Reconciliation",
                    requiredPermissionName: IdentityConsts.ITAdminPermissionName
        ));

        // Add Reporting Configuration menu item for IT Admin users
        context.Menu.AddItem(
           new ApplicationMenuItem(
                   ReportingMenus.Prefix,
                   displayName: "Reporting",
                   "~/ReportingAdmin/Configuration",
                   requiredPermissionName: IdentityConsts.ITAdminPermissionName
       ));
        return Task.CompletedTask;
    }
}
