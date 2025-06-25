using System.Collections.Generic;
using System.ComponentModel;
using System;

namespace Unity.GrantManager.Web.Pages.BulkApprovals.ViewModels
{
    public class BulkApplicationApprovalViewModel
    {
        public BulkApplicationApprovalViewModel()
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
        public List<ApprovalNoteViewModel> Notes { get; set; }
        public bool? IsDirectApproval { get; internal set; }

        [DisplayName("Recommended Amount")]
        public decimal RecommendedAmount { get; internal set; }
    }
}
