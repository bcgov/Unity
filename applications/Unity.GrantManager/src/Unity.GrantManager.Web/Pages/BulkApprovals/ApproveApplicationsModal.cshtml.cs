using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.BulkApprovals;

public class ApproveApplicationsModalModel(IBulkApprovalsAppService bulkApprovalsAppService,
    BrowserUtils browserUtils) : AbpPageModel
{
    [BindProperty]
    public List<BulkApplicationApproval>? BulkApplicationApprovals { get; set; }

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
            var bulkApproval = new BulkApplicationApproval
            {
                ApplicationId = application.ApplicationId,
                ReferenceNo = application.ReferenceNo,
                ApplicantName = application.ApplicantName,
                DecisionDate = application.FinalDecisionDate ?? DateTime.UtcNow.AddMinutes(-offsetMinutes),
                RequestedAmount = application.RequestedAmount,
                ApprovedAmount = application.ApprovedAmount == 0m ? application.RequestedAmount : application.ApprovedAmount,
                ApplicationStatus = application.ApplicationStatus,
                FormName = application.FormName,
                IsValid = application.IsValid,
                Notes = SetNotesForApplication(application)
            };

            BulkApplicationApprovals.Add(bulkApproval);
        }

        Invalid = applications.Exists(s => !s.IsValid) || MaxBatchCountExceeded;
        ApplicationsCount = applications.Count;
    }

    private List<ApprovalNote> SetNotesForApplication(BulkApprovalDto application)
    {
        var notes = new List<ApprovalNote>
        {
            new("DECISION_DATE_DEFAULTED", false, L.GetString("ApplicationBatchApprovalRequest:DecisionDateDefaulted"), false),
            new("APPROVED_AMOUNT_DEFAULTED", false, L.GetString("ApplicationBatchApprovalRequest:ApprovedAmountDefaulted"), false),
            new("INVALID_STATUS", false, L.GetString("ApplicationBatchApprovalRequest:InvalidStatus"), true),
            new("INVALID_PERMISSIONS", false, L.GetString("ApplicationBatchApprovalRequest:InvalidPermissions"), true),
            new("INVALID_APPROVED_AMOUNT", false, L.GetString("ApplicationBatchApprovalRequest:InvalidApprovedAmount"), true)
        };

        if (application.FinalDecisionDate == null)
        {
            notes[0] = new ApprovalNote(notes[0].Key, true, notes[0].Description, notes[0].IsError);
        }

        if (application.ApprovedAmount == 0m)
        {
            notes[0] = new ApprovalNote(notes[1].Key, true, notes[1].Description, notes[1].IsError);
        }

        foreach (var validation in application.ValidationMessages)
        {
            var index = notes.FindIndex(note => note.Key == validation);
            if (index != -1)
            {
                notes[index] = new ApprovalNote(validation, true, notes[index].Description, notes[index].IsError);
            }
        }

        return notes;
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

    public class BulkApplicationApproval
    {
        public BulkApplicationApproval()
        {
            Notes = [];
        }

        public Guid ApplicationId { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string? ApplicantName { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;

        [DisplayName("Requested Amount")]
        public decimal RequestedAmount { get; set; } = 0m;

        [DisplayName("Approved Amount")]
        public decimal ApprovedAmount { get; set; } = 0m;

        [DisplayName("Decision Date")]
        public DateTime DecisionDate { get; set; }
        public bool IsValid { get; set; }
        public List<ApprovalNote> Notes { get; set; }
    }

    public class ApprovalNote
    {
        public ApprovalNote(string key, bool active, string description, bool isError)
        {
            Key = key;
            Active = active;
            Description = description;
            IsError = isError;
        }

        public string Key { get; set; }
        public bool Active { get; set; }
        public string Description { get; set; }
        public bool IsError { get; set; }
    }
}
