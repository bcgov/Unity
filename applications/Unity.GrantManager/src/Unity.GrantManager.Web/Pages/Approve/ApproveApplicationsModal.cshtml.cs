using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Unity.GrantManager.Web.Pages.Approve;

public class ApproveApplicationsModalModel : AbpPageModel
{
        
    [TempData]
    public string selectedApplicationIds { get; set; } = "";

    public List<SelectListItem> statusList { get; set; }

    private readonly IApplicationStatusService _statusService;
    private readonly GrantApplicationAppService _applicationService;

    public ApproveApplicationsModalModel(IApplicationStatusService statusService, GrantApplicationAppService applicationService)
    {
        _statusService = statusService;
        _applicationService = applicationService;
    }

    public async Task OnGetAsync(string applicationIds)
    {
        selectedApplicationIds = applicationIds;
    }     

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Guid statusId = Guid.Empty;
            var statuses = await _statusService.GetListAsync();
            var approvedStatus = statuses.FirstOrDefault(status => status.StatusCode == "GRANT_APPROVED");
            if (approvedStatus != null)
            {
                statusId = approvedStatus.Id;                
            }
            else
            {
                throw new Exception("Grant Approved status is not found in the database!");
            }

            var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(selectedApplicationIds);           
            await _applicationService.UpdateApplicationStatus(applicationIds.ToArray(), statusId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating application statuses");
        }
        return NoContent();
    }
}
