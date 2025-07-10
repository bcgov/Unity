using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Pages.BulkApprovals.ViewModels;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.BulkApprovals;

public class ApproveApplicationsModalModel(IBulkApprovalsAppService bulkApprovalsAppService,
    BrowserUtils browserUtils) : AbpPageModel
{
    [BindProperty]
    public List<BulkApplicationApprovalViewModel>? BulkApplicationApprovals { get; set; }

    [TempData]
    public int ApplicationsCount { get; set; }

    [TempData]
    public bool Invalid { get; set; }

    [TempData]
    public int MaxBatchCount { get; set; }

    [TempData]
    public string? MaxBatchCountExceededError { get; set; }

    [TempData]
    public bool MaxBatchCountExceeded { get; set; }

    public async Task OnGetAsync(string applicationIds)
    {
        MaxBatchCount = BatchApprovalConsts.MaxBatchCount;
        BulkApplicationApprovals = [];
        MaxBatchCountExceededError = L["ApplicationBatchApprovalRequest:MaxCountExceeded", BatchApprovalConsts.MaxBatchCount.ToString()].Value;

        Guid[] applicationGuids = ParseApplicationIds(applicationIds);

        if (!ValidCount(applicationGuids))
        {
            MaxBatchCountExceeded = true;
        }

        if (applicationGuids.Length == 0)
        {
            return;
        }

        // Load the applications by Id
        var applications = await bulkApprovalsAppService.GetApplicationsForBulkApproval(applicationGuids);
        var offsetMinutes = browserUtils.GetBrowserOffset();

        foreach (var application in applications)
        {
            var bulkApproval = new BulkApplicationApprovalViewModel
            {
                ApplicationId = application.ApplicationId,
                ReferenceNo = application.ReferenceNo,
                ApplicantName = application.ApplicantName,
                DecisionDate = application.FinalDecisionDate ?? DateTime.UtcNow.AddMinutes(-offsetMinutes),
                RequestedAmount = application.RequestedAmount,
                RecommendedAmount = application.RecommendedAmount,
                ApprovedAmount = application.ApprovedAmount,
                ApplicationStatus = application.ApplicationStatus,
                FormName = application.FormName,
                IsValid = application.IsValid,
                IsDirectApproval = application.IsDirectApproval         
            };

            SetNotes(application, bulkApproval);
            BulkApplicationApprovals.Add(bulkApproval);
        }

        Invalid = applications.Exists(s => !s.IsValid) || MaxBatchCountExceeded;
        ApplicationsCount = applications.Count;
    }

    private void SetNotes(BulkApprovalDto application, BulkApplicationApprovalViewModel bulkApproval)
    {
        /* 
        * 0 - Decision Date Defaulted
        * 1 - Approved Amount Defaulted
        * 2 - Invalid Status
        * 3 - Invalid Permissions
        * 4 - Invalid Approved Amount
        * 5 - Invalid Recommended Amount
        */

        List<ApprovalNoteViewModel> notes = ApprovalNoteViewModel.CreateNotesList(localizer: L);

        if (application.FinalDecisionDate == null)
        {
            notes[0] = new ApprovalNoteViewModel(notes[0].Key, true, notes[0].Description, notes[0].IsError);
        }

        if (bulkApproval.ApprovedAmount == 0m) // this will be defaulted either way if is 0
        {
            notes[1] = new ApprovalNoteViewModel(notes[1].Key, true, notes[1].Description, notes[1].IsError);
        }

        foreach (var validation in application.ValidationMessages)
        {
            var index = notes.FindIndex(note => note.Key == validation);
            if (index != -1)
            {
                notes[index] = new ApprovalNoteViewModel(validation, true, notes[index].Description, notes[index].IsError);
            }
        }

        bulkApproval.Notes = notes;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (BulkApplicationApprovals == null) return NoContent();

            var approvalRequests = MapBulkApprovalRequests();

            // Fire off request to approve the applications
            var result = await bulkApprovalsAppService.BulkApproveApplications(approvalRequests);

            // Return the result out
            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating application statuses");
        }

        return NoContent();
    }

    private List<BulkApprovalDto> MapBulkApprovalRequests()
    {
        var bulkApprovals = new List<BulkApprovalDto>();

        foreach (var application in BulkApplicationApprovals ?? [])
        {
            bulkApprovals.Add(new BulkApprovalDto()
            {
                ApplicantName = application.ApplicantName ?? string.Empty,
                ApplicationId = application.ApplicationId,
                ApprovedAmount = application.ApprovedAmount,
                RecommendedAmount = application.RecommendedAmount,
                IsDirectApproval = application.IsDirectApproval,
                FinalDecisionDate = application.DecisionDate,
                ReferenceNo = application.ReferenceNo,
                RequestedAmount = application.RequestedAmount,
                ValidationMessages = []
            });
        }

        return bulkApprovals;
    }

    private static Guid[] ParseApplicationIds(string applicationIds)
    {
        return JsonConvert.DeserializeObject<Guid[]>(applicationIds ?? string.Empty) ?? [];
    }

    private bool ValidCount(Guid[] applicationGuids)
    {
        // Soft check in the UI for max approvals in one batch, this is subject to be tweaked later after performance testing
        return applicationGuids.Length <= MaxBatchCount;
    }
}
