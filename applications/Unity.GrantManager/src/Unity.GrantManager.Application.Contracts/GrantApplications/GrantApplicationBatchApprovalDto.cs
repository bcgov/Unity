using System;
using System.Collections.Generic;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationBatchApprovalDto
    {
        public GrantApplicationBatchApprovalDto()
        {
            ValidationMessages = [];
            ReferenceNo = string.Empty;
            ApplicantName = string.Empty;
        }

        public List<string> ValidationMessages { get; set; }
        public bool IsValid => ValidationMessages.Count == 0;

        public Guid ApplicationId { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal RequestedAmount { get; set; }
        public DateTime? FinalDecisionDate { get; set; }
        public string ReferenceNo { get; set; }
        public string ApplicantName { get; set; }
    }
}
