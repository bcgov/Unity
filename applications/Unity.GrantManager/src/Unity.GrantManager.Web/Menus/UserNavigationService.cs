using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.Modules.Shared.Permissions;
using Unity.Payments.Web.Menus;
using Unity.TenantManagement.Web.Navigation;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Menus;

// Registered per request so menu permissions are evaluated again after login, tenant changes
// and permission changes. API controllers and application services do not use this service.
public class UserNavigationService(
    IMenuManager menuManager,
    ICurrentUser currentUser,
    IPermissionChecker permissionChecker)
{
    public const string NoAccessUrl = "/Account/NoGrantPrograms";

    private Task<ApplicationMenu>? _mainMenu;

    // A page path is accessible when any of its menu items is visible. The most specific
    // (longest) matching path wins, so a page entry overrides its folder's entry.
    private static readonly Dictionary<string, string[]> PageMenus = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/GrantApplications"] = [GrantManagerMenus.Applications],
        // Onboarding tenants hide Applications, but each onboarding request is an Application
        // opened from the Onboarding queue. Only the details page is opened that way; the
        // application list and any other /GrantApplications page still need Applications.
        ["/GrantApplications/Details"] = [GrantManagerMenus.Applications, TenantManagementMenuNames.Onboarding],
        ["/Applicants"] = [GrantManagerMenus.Applicants],
        ["/Intakes"] = [GrantManagerMenus.Intakes],
        ["/ApplicationForms"] = [GrantManagerMenus.ApplicationForms],
        ["/FormConfiguration"] = [GrantManagerMenus.ApplicationForms],
        ["/Dashboard"] = [GrantManagerMenus.Dashboard],
        ["/PaymentRequests"] = [PaymentsMenus.Prefix],
        ["/PaymentHistory"] = [PaymentsMenus.Prefix]
    };

    public async Task<string?> GetLandingPageAsync()
    {
        // Host administrators have a role-based route independent of tenant permissions.
        if (currentUser.IsInRole(IdentityConsts.ITAdminRoleName))
        {
            return "/TenantManagement/Tenants";
        }

        var menu = await GetMainMenuAsync();
        var url = FindFirstUrl(menu.Items);
        if (url != null)
        {
            return url;
        }

        // Configuration is available from the toolbar rather than the main navigation.
        return await permissionChecker.IsGrantedAsync(UnitySettingManagementPermissions.UserInterface)
            ? "/ConfigurationManagement"
            : null;
    }

    public async Task<bool> CanAccessPageAsync(string pagePath)
    {
        if (currentUser.IsInRole(IdentityConsts.ITAdminRoleName))
        {
            return true;
        }

        var menuNames = PageMenus
            .Where(entry => pagePath.Equals(entry.Key, StringComparison.OrdinalIgnoreCase)
                || pagePath.StartsWith(entry.Key + "/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(entry => entry.Key.Length)
            .Select(entry => entry.Value)
            .FirstOrDefault();

        if (menuNames == null)
        {
            // Other pages retain their existing authorization policies.
            return true;
        }

        var menu = await GetMainMenuAsync();
        return menuNames.Any(menuName => ContainsItem(menu.Items, menuName));
    }

    private Task<ApplicationMenu> GetMainMenuAsync()
        => _mainMenu ??= menuManager.GetMainMenuAsync();

    private static string? FindFirstUrl(IEnumerable<ApplicationMenuItem> items)
    {
        foreach (var item in items.OrderBy(item => item.Order))
        {
            if (item.IsDisabled)
            {
                continue;
            }

            var url = item.Url;
            if (url?.StartsWith("~/", StringComparison.Ordinal) == true)
            {
                url = url[1..];
            }

            if (url?.StartsWith("/", StringComparison.Ordinal) == true
                && !url.StartsWith("//", StringComparison.Ordinal)
                && !url.StartsWith("/\\", StringComparison.Ordinal)
                && url != "/"
                && item.Name != GrantManagerMenus.Home)
            {
                return url;
            }

            var childUrl = FindFirstUrl(item.Items);
            if (childUrl != null)
            {
                return childUrl;
            }
        }

        return null;
    }

    private static bool ContainsItem(IEnumerable<ApplicationMenuItem> items, string name)
        => items.Any(item => !item.IsDisabled
            && (item.Name == name || ContainsItem(item.Items, name)));
}
