using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Approve;

public class ApproveApplicationsModalModel(IApplicationApprovalService applicationApprovalService) : AbpPageModel
{
    [BindProperty]
    public string? SelectedApplicationIds { get; set; }

    [BindProperty]
    public List<BulkApplicationApproval>? BulkApplicationApprovals { get; set; }

    [TempData]
    public List<string>? PopupMessages { get; set; }

    [TempData]
    public string? PopupTitle { get; set; }

    [TempData]
    public bool Invalid { get; set; }

    private const int _maxBatchCount = 50;

    public async void OnGet(string applicationIds)
    {
        SelectedApplicationIds = applicationIds;
        PopupMessages = [];
        Invalid = false;
        BulkApplicationApprovals = [];

        Guid[] applicationGuids = ParseApplicationIds();

        if (!ValidCount(applicationGuids))
        {
            PopupMessages.Add($"You can only approve {_maxBatchCount} applications at a time. Please select fewer applications.");
            Invalid = true;
        }

        // Load the applications by Id
        var applications = await applicationApprovalService.GetApplicationsForBulkApproval(ParseApplicationIds());

        foreach (var application in applications)
        {
            BulkApplicationApprovals.Add(new BulkApplicationApproval()
            {
                ApplicationId = application.ApplicationId,
                ReferenceNo = application.ReferenceNo,
                Errors = [.. application.ValidationMessages],
                ApplicantName = application.ApplicantName
            });
        }
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

    private static bool ValidCount(Guid[] applicationGuids)
    {
        // Soft check in the UI for max approvals in one batch, this is subject to be tweaked later after performance testing
        return applicationGuids.Length <= _maxBatchCount;
    }

    public class BulkApplicationApproval
    {
        public BulkApplicationApproval()
        {
            Errors = [];
        }

        public Guid ApplicationId { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public bool Disabled => Errors.Length > 0;
        public string[] Errors { get; set; }
    }
}
