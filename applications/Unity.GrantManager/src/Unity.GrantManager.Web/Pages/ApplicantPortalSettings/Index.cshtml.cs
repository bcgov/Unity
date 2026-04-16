using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Pages.ApplicantPortalSettings;

[Authorize]
public class IndexModel(IApplicationStatusService applicationStatusService) : GrantManagerPageModel
{
    public IList<ApplicationStatusDto> Statuses { get; set; } = [];

    public async Task OnGetAsync()
    {
        Statuses = await applicationStatusService.GetListAsync();
    }
}
