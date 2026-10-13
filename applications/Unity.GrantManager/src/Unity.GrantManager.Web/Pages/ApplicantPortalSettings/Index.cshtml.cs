using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.ApplicantPortal;

namespace Unity.GrantManager.Web.Pages.ApplicantPortalSettings;

[Authorize]
public class IndexModel(IApplicationStatusService applicationStatusService,
    IApplicantPortalMessageAppService messageService) : GrantManagerPageModel
{
    public MultipleIdentitiesMessageDto MultipleIdentitiesMessage { get; set; } = new();

    public IList<ApplicantPortalStatusDto> Statuses { get; set; } = [];

    public async Task OnGetAsync()
    {
        MultipleIdentitiesMessage = await messageService.GetMultipleIdentitiesAsync();
        Statuses = await applicationStatusService.GetApplicantPortalStatusListAsync();
    }
}
