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
                ApplicationStatus = application.ApplicationStatus,
                FormName = application.FormName,
                IsValid = application.IsValid,
                IsDirectApproval = application.IsDirectApproval,
                //Notes = SetNotesForApplication(application), // We need to set the notes upfront before setting Recommended Amount and Approved Amount
                //RecommendedAmount = application.RecommendedAmount,
                //ApprovedAmount = SetApprovedAmount(application, application.IsDirectApproval),
            };

            SetNotesAndAmounts(application, bulkApproval);
            BulkApplicationApprovals.Add(bulkApproval);
        }

        Invalid = applications.Exists(s => !s.IsValid) || MaxBatchCountExceeded;
        ApplicationsCount = applications.Count;
    }

    private void SetNotesAndAmounts(BulkApprovalDto application, BulkApplicationApproval bulkApproval)
    {
        /* 
        * 0 - Decision Date
        * 1 - Approved Amount Defaulted
        * 2 - Invalid Status
        * 3 - Invalid Permissions
        * 4 - Invalid Approved Amount
        * 5 - Invalid Recommended Amount
        */

        List<ApprovalNote> notes = CreateNotesList();

        if (application.FinalDecisionDate == null)
        {
            notes[0] = new ApprovalNote(notes[0].Key, true, notes[0].Description, notes[0].IsError);
        }

        if (bulkApproval.ApprovedAmount == 0m) // this will be defaulted either way
        {
            notes[1] = new ApprovalNote(notes[1].Key, true, notes[1].Description, notes[1].IsError);
        }

        /*
         *  if directApproval = true the populate from the requested amount
         *  if directApproval = false or null then populate from recommended amount
        */

        if (application.IsDirectApproval == true)
        {
            bulkApproval.ApprovedAmount = application.ApprovedAmount == 0m ? application.RequestedAmount : application.ApprovedAmount;
        }
        else // this is null or false
        {
            // If the recommended amount is 0 we need to show an error
            if (application.RecommendedAmount == 0m)
            {
                notes[5] = new ApprovalNote(notes[5].Key, true, notes[5].Description, notes[5].IsError);
            }

            bulkApproval.ApprovedAmount = application.ApprovedAmount == 0m ? application.RecommendedAmount : application.ApprovedAmount;
        }

        // If approved amount is still 0.00 after default sets then it is an error
        if (bulkApproval.ApprovedAmount == 0m)
        {
            notes[4] = new ApprovalNote(notes[4].Key, true, notes[4].Description, notes[4].IsError);
        }

        foreach (var validation in application.ValidationMessages)
        {
            var index = notes.FindIndex(note => note.Key == validation);
            if (index != -1)
            {
                notes[index] = new ApprovalNote(validation, true, notes[index].Description, notes[index].IsError);
            }
        }

        bulkApproval.Notes = notes;
    }

    private List<ApprovalNote> CreateNotesList()
    {
        return
        [
            new("DECISION_DATE_DEFAULTED", false, L.GetString("ApplicationBatchApprovalRequest:DecisionDateDefaulted"), false),
            new("APPROVED_AMOUNT_DEFAULTED", false, L.GetString("ApplicationBatchApprovalRequest:ApprovedAmountDefaulted"), false),
            new("INVALID_STATUS", false, L.GetString("ApplicationBatchApprovalRequest:InvalidStatus"), true),
            new("INVALID_PERMISSIONS", false, L.GetString("ApplicationBatchApprovalRequest:InvalidPermissions"), true),
            new("INVALID_APPROVED_AMOUNT", false, L.GetString("ApplicationBatchApprovalRequest:InvalidApprovedAmount"), true),
            new("INVALID_RECOMMENDED_AMOUNT", false, L.GetString("ApplicationBatchApprovalRequest:InvalidRecommendedAmount"), true)
        ];
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
        public bool? IsDirectApproval { get; internal set; }

        [DisplayName("Recommended Amount")]
        public decimal RecommendedAmount { get; internal set; }
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
