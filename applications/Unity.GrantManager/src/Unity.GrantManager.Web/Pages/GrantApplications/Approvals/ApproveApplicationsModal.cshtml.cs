using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.GrantApplications.Approvals;

public class ApproveApplicationsModalModel(IBulkApprovalsAppService bulkApprovalsAppService,
    BrowserUtils browserUtils) : AbpPageModel
{
    [BindProperty]
    public List<BulkApplicationApproval>? BulkApplicationApprovals { get; set; }

    [TempData]
    public List<string>? PopupMessages { get; set; }

    [TempData]
    public string? PopupTitle { get; set; }

    [TempData]
    public int ApplicationsCount { get; set; }

    [TempData]
    public bool Invalid { get; set; }

    private const int _maxBatchCount = 50;

    public async void OnGet(string applicationIds)
    {
        PopupMessages = [];
        Invalid = false;
        BulkApplicationApprovals = [];

        Guid[] applicationGuids = ParseApplicationIds(applicationIds);

        if (!ValidCount(applicationGuids))
        {
            PopupMessages.Add($"You can only approve {_maxBatchCount} applications at a time. Please select fewer applications.");
            Invalid = true;
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

        Invalid = applications.Exists(s => !s.IsValid);
        ApplicationsCount = applications.Count;
    }

    private static List<KeyValuePair<string, bool>> SetNotesForApplication(BulkApprovalDto application)
    {
        var notes = new List<KeyValuePair<string, bool>>
        {
            new("DECISION_DATE_DEFAULTED", false),
            new("APPROVED_AMOUNT_DEFAULTED", false),
            new("INVALID_STATUS", false),
            new("INVALID_PERMISSIONS", false),
            new("INVALID_APPROVED_AMOUNT", false)
        };


        if (application.FinalDecisionDate == null)
        {
            notes[0] = new KeyValuePair<string, bool>("DECISION_DATE_DEFAULTED", true);
        }

        if (application.ApprovedAmount == 0m)
        {
            notes[1] = new KeyValuePair<string, bool>("APPROVED_AMOUNT_DEFAULTED", true);
        }

        foreach (var validation in application.ValidationMessages)
        {
            for (int i = 2; i < notes.Count; i++)
            {
                if (notes[i].Key == validation)
                {
                    notes[i] = new KeyValuePair<string, bool>(notes[i].Key, true);
                }
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
                ApplicantName = application.ApplicantName,
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

    private static bool ValidCount(Guid[] applicationGuids)
    {
        // Soft check in the UI for max approvals in one batch, this is subject to be tweaked later after performance testing
        return applicationGuids.Length <= _maxBatchCount;
    }

    public class BulkApplicationApproval
    {
        public BulkApplicationApproval()
        {
            Notes = [];
        }

        public Guid ApplicationId { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string FormName { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;

        [DisplayName("Requested Amount")]
        public decimal RequestedAmount { get; set; } = 0m;

        [DisplayName("Approved Amount")]
        public decimal ApprovedAmount { get; set; } = 0m;

        [DisplayName("Decision Date")]
        public DateTime DecisionDate { get; set; }
        public bool IsValid { get; set; }
        public List<KeyValuePair<string, bool>> Notes { get; set; }
    }
}
