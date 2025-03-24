using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Approve;

public class ApproveApplicationsModalModel(IApplicationApprovalService applicationApprovalService) : AbpPageModel
{
    [BindProperty]
    public string? SelectedApplicationIds { get; set; }

    [BindProperty]
    public string? OperationStatusCode { get; set; }

    [TempData]
    public string? PopupMessage { get; set; }

    [TempData]
    public string? PopupTitle { get; set; }

    public async void OnGet(string applicationIds, string operation, string message, string title)
    {
        SelectedApplicationIds = applicationIds;
        OperationStatusCode = operation;
        PopupMessage = message;
        PopupTitle = title;

        // Load the applications by Id
        var applications = await applicationApprovalService.GetApplicationsForBulkApproval(ParseApplicationIds());
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Fire off request to approve the applications
            var result = await applicationApprovalService.BulkApproveApplications(ParseApplicationIds());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating application statuses");
        }
        return NoContent();
    }

    private Guid[] ParseApplicationIds()
    {
        return JsonConvert.DeserializeObject<Guid[]>(SelectedApplicationIds ?? string.Empty) ?? [];
    }
}
