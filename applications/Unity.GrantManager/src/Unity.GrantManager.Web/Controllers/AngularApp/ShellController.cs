using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Ui.Branding;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Controllers.AngularApp;

// Shell-wide chrome (menu/branding/user) for the Angular SPA, separate from any one
// feature area's own bootstrap endpoint (e.g. ConfigurationManagementController).
// Bare [Authorize]: main menu items are already individually permission-filtered by
// IMenuManager (see MainNavbarMenuViewComponent, which trusts it the same way), so
// this endpoint itself isn't behind any specific permission - any authenticated user
// gets back whatever menu their own permissions already leave them with.
[Route("api/angular-app/shell")]
[ApiController]
[Authorize]
public class ShellController : ControllerBase
{
    private readonly IMenuManager _menuManager;
    private readonly IBrandingProvider _brandingProvider;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly IFeatureChecker _featureChecker;

    public ShellController(
        IMenuManager menuManager,
        IBrandingProvider brandingProvider,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IFeatureChecker featureChecker)
    {
        _menuManager = menuManager;
        _brandingProvider = brandingProvider;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _featureChecker = featureChecker;
    }

    [HttpGet("bootstrap")]
    public async Task<ActionResult<ShellBootstrapDto>> GetBootstrapAsync()
    {
        var menu = await _menuManager.GetMainMenuAsync();

        // Mirrors Topbar/Default.cshtml's inline @if conditions exactly - these items
        // aren't part of the IMenuManager tree, so their visibility has to be
        // recomputed here the same way the Razor view does it.
        var isAuthorizedForTenantSwitch = _currentUser.IsAuthenticated
            && _currentUser.FindClaimValue("has_multiple_tenants") == "true";

        var isSystemAdmin = _currentUser.FindClaims(AbpClaimTypes.Role).Any(c => c.Value == "system_admin");
        var showConfigurationManagement = isSystemAdmin && await _featureChecker.IsEnabledAsync("SettingManagement.Enable");

        return Ok(new ShellBootstrapDto
        {
            Branding = new BrandingDto
            {
                AppName = _brandingProvider.AppName,
                LogoUrl = _brandingProvider.LogoUrl
            },
            User = new ShellUserDto
            {
                Badge = _currentUser.FindClaimValue("Badge"),
                CurrentTenantName = _currentTenant.Name
            },
            MainMenu = ProjectMenuItems(menu.Items),
            UserDropdown = new UserDropdownDto
            {
                ShowSwitchGrantPrograms = isAuthorizedForTenantSwitch,
                ShowApplicantPortalConfiguration = _currentTenant.Id != null,
                ShowConfigurationManagement = showConfigurationManagement,
                ShowUnityAdmin = _currentUser.IsInRole("ITOperations")
            }
        });
    }

    private static List<ShellMenuItemDto> ProjectMenuItems(IEnumerable<ApplicationMenuItem> items)
    {
        return items.Select(item => new ShellMenuItemDto
        {
            Name = item.Name,
            DisplayName = item.DisplayName,
            Url = NormalizeUrl(item.Url),
            Icon = item.Icon,
            Items = ProjectMenuItems(item.Items)
        }).ToList();
    }

    // GrantManagerMenuContributor.cs registers items with tilde-relative URLs
    // ("~/GrantApplications"), which Razor's Menu/Default.cshtml resolves via
    // Url.Content(...) before rendering an <a href>. Angular has no equivalent -
    // an unresolved "~/..." string is not an absolute path, so the browser treats
    // it as relative to the CURRENT page and a click never leaves /app/*, which is
    // how this produced "NG04002: Cannot match any routes" instead of navigating
    // to the MVC app. This app has no PathBase, so stripping the "~" is equivalent.
    private static string? NormalizeUrl(string? url)
    {
        return url?.StartsWith('~') == true ? url[1..] : url;
    }
}

public class ShellBootstrapDto
{
    public BrandingDto Branding { get; set; } = new();
    public ShellUserDto User { get; set; } = new();
    public List<ShellMenuItemDto> MainMenu { get; set; } = new();
    public UserDropdownDto UserDropdown { get; set; } = new();
}

public class BrandingDto
{
    public string AppName { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
}

public class ShellUserDto
{
    public string? Badge { get; set; }
    public string? CurrentTenantName { get; set; }
}

public class ShellMenuItemDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Icon { get; set; }
    public List<ShellMenuItemDto> Items { get; set; } = new();
}

public class UserDropdownDto
{
    public bool ShowSwitchGrantPrograms { get; set; }
    public bool ShowApplicantPortalConfiguration { get; set; }
    public bool ShowConfigurationManagement { get; set; }
    public bool ShowUnityAdmin { get; set; }
}
