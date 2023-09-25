using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Approve;

public class ApproveApplicationsModalModel : AbpPageModel
{

    [BindProperty]
    public string SelectedApplicationIds { get; set; } = "";
    [BindProperty]
    public string OperationStatusCode { get; set; } = "";
    [TempData]
    public string PopupMessage { get; set; } = "";
    [TempData]
    public string PopupTitle { get; set; } = "";

    public List<SelectListItem> StatusList { get; set; } = new();

    private readonly IApplicationStatusService _statusService;
    private readonly GrantApplicationAppService _applicationService;

    public ApproveApplicationsModalModel(IApplicationStatusService statusService, GrantApplicationAppService applicationService)
    {
        _statusService = statusService;
        _applicationService = applicationService;
    }

    public void OnGet(string applicationIds, string operation, string message, string title)
    {
        SelectedApplicationIds = applicationIds;
        OperationStatusCode = operation;
        PopupMessage = message;
        PopupTitle = title;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            Guid statusId;
            var statuses = await _statusService.GetListAsync();
            var approvedStatus = statuses.FirstOrDefault(status => status.StatusCode == OperationStatusCode);
            if (approvedStatus != null)
            {
                statusId = approvedStatus.Id;
            }
            else
            {
                throw new ArgumentException(OperationStatusCode + " status code is not found in the database!");
            }

            var applicationIds = JsonConvert.DeserializeObject<List<Guid>>(SelectedApplicationIds);
            if (null != applicationIds)
            {
                await _applicationService.UpdateApplicationStatus(applicationIds.ToArray(), statusId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating application statuses");
        }
        return NoContent();
    }
}
