using System;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications
{
    public class BulkApprovalDto
    {
        public BulkApprovalDto()
        {
            ValidationMessages = [];
            ReferenceNo = string.Empty;
            ApplicantName = string.Empty;
            FormName = string.Empty;
            ApplicationStatus = string.Empty;
        }

        public List<string> ValidationMessages { get; set; }
        public bool IsValid { get;set; }

        public Guid ApplicationId { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal RequestedAmount { get; set; }
        public DateTime? FinalDecisionDate { get; set; }
        public string ReferenceNo { get; set; }
        public string ApplicantName { get; set; }
        public string FormName { get; set; }
        public string ApplicationStatus { get; set; }
        public bool? IsDirectApproval { get; set; }
        public decimal RecommendedAmount { get; set; }
    }
}
