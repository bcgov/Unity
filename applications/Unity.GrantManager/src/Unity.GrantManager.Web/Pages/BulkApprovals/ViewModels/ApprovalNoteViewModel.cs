using Microsoft.Extensions.Localization;
using System.Collections.Generic;

namespace Unity.GrantManager.Web.Pages.BulkApprovals.ViewModels
{
    public class ApprovalNoteViewModel
    {
        public ApprovalNoteViewModel(string key, bool active, string description, bool isError)
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

        public static List<ApprovalNoteViewModel> CreateNotesList(IStringLocalizer localizer)
        {
            return
            [
                new("DECISION_DATE_DEFAULTED", false, localizer.GetString("ApplicationBatchApprovalRequest:DecisionDateDefaulted"), false),
                new("APPROVED_AMOUNT_DEFAULTED", false, localizer.GetString("ApplicationBatchApprovalRequest:ApprovedAmountDefaulted"), false),
                new("INVALID_STATUS", false, localizer.GetString("ApplicationBatchApprovalRequest:InvalidStatus"), true),
                new("INVALID_PERMISSIONS", false, localizer.GetString("ApplicationBatchApprovalRequest:InvalidPermissions"), true),
                new("INVALID_APPROVED_AMOUNT", false, localizer.GetString("ApplicationBatchApprovalRequest:InvalidApprovedAmount"), true),
                new("INVALID_RECOMMENDED_AMOUNT", false, localizer.GetString("ApplicationBatchApprovalRequest:InvalidRecommendedAmount"), true)
            ];
        }
    }
}
