using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;

namespace Unity.GrantManager.Web.Pages.ApplicantPortalSettings;

[Authorize]
public class IndexModel(
    IApplicationStatusService applicationStatusService,
    IPermissionChecker permissionChecker) : GrantManagerPageModel
{
    public IList<ApplicantPortalStatusDto> Statuses { get; set; } = [];
    public bool CanEditProgramDetails { get; set; }
    public ApplicantPortalProgramDetailsDto ProgramDetails { get; set; } = new();

    public async Task OnGetAsync()
    {
        Statuses = await applicationStatusService.GetApplicantPortalStatusListAsync();
        CanEditProgramDetails = await permissionChecker.IsGrantedAsync(GrantManagerPermissions.ApplicantPortal.EditProgramDetails);

        if (CanEditProgramDetails)
        {
            ProgramDetails = await applicationStatusService.GetApplicantPortalProgramDetailsAsync();
        }
    }
}
