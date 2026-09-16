using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.AI.Permissions;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.SettingManagement;
using Unity.Modules.Shared;
using Unity.Notifications.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Controllers.AngularApp;

// BFF-style layer for the Unity.GrantManager.Angular SPA (served separately, same origin
// via an OpenShift Route path split - see the strangler-fig migration plan). Wraps the
// existing IProgramDetailsAppService rather than exposing ABP's auto-generated conventional
// endpoint directly, so the SPA has a contract this project controls.
[Route("api/angular-app/configuration-management")]
[ApiController]
[Authorize(UnitySettingManagementPermissions.UserInterface)]
public class ConfigurationManagementController : ControllerBase
{
    private readonly IProgramDetailsAppService _programDetailsAppService;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IFeatureChecker _featureChecker;

    public ConfigurationManagementController(
        IProgramDetailsAppService programDetailsAppService,
        IPermissionChecker permissionChecker,
        IFeatureChecker featureChecker)
    {
        _programDetailsAppService = programDetailsAppService;
        _permissionChecker = permissionChecker;
        _featureChecker = featureChecker;
    }

    // Mirrors Pages/ConfigurationManagement/Index.cshtml.cs's IndexModel.OnGetAsync()
    // exactly (same feature/permission checks) - the Angular side-menu needs the same
    // "what should this user see" flags the Razor page's side-menu already computes,
    // since only Program Details is migrated today and the rest still redirect back
    // to the MVC page's own sections.
    [HttpGet("bootstrap")]
    public async Task<ActionResult<ConfigurationManagementBootstrapDto>> GetBootstrapAsync()
    {
        var showNotifications = await _featureChecker.IsEnabledAsync("Unity.Notifications")
            && await _permissionChecker.IsGrantedAsync(NotificationsPermissions.Settings);

        var isPaymentsFeatureEnabled = await _featureChecker.IsEnabledAsync("Unity.Payments");
        var isAuthorizedForPaymentConfiguration = await _permissionChecker.IsGrantedAsync(UnitySettingManagementPermissions.ConfigurePayments);
        var showPayments = isPaymentsFeatureEnabled && isAuthorizedForPaymentConfiguration;

        var showCustomFields = await _featureChecker.IsEnabledAsync("Unity.Flex");
        var showScoresheets = await _featureChecker.IsEnabledAsync("Unity.Flex");

        var showTags = await _permissionChecker.IsGrantedAsync(UnitySelector.SettingManagement.Tags.Default);

        var showAI = await _featureChecker.IsEnabledAsync("Unity.AI.Scoring")
            && await _permissionChecker.IsGrantedAsync(AIPermissions.Configuration.ConfigureAI);

        var showProgramDetails = await _permissionChecker.IsGrantedAsync(UnitySettingManagementPermissions.EditProgramDetails);

        return Ok(new ConfigurationManagementBootstrapDto
        {
            ShowNotifications = showNotifications,
            ShowPayments = showPayments,
            ShowCustomFields = showCustomFields,
            ShowScoresheets = showScoresheets,
            ShowTags = showTags,
            ShowAI = showAI,
            ShowProgramDetails = showProgramDetails
        });
    }

    [HttpGet("program-details")]
    [Authorize(UnitySettingManagementPermissions.EditProgramDetails)]
    public async Task<ActionResult<ProgramDetailsDto>> GetProgramDetailsAsync()
    {
        return Ok(await _programDetailsAppService.GetProgramDetailsAsync());
    }

    [HttpPut("program-details")]
    [Authorize(UnitySettingManagementPermissions.EditProgramDetails)]
    public async Task<IActionResult> UpdateProgramDetailsAsync(UpdateProgramDetailsDto input)
    {
        await _programDetailsAppService.UpdateProgramDetailsAsync(input);
        return NoContent();
    }
}

public class ConfigurationManagementBootstrapDto
{
    public bool ShowNotifications { get; set; }
    public bool ShowPayments { get; set; }
    public bool ShowCustomFields { get; set; }
    public bool ShowScoresheets { get; set; }
    public bool ShowTags { get; set; }
    public bool ShowAI { get; set; }
    public bool ShowProgramDetails { get; set; }
}
