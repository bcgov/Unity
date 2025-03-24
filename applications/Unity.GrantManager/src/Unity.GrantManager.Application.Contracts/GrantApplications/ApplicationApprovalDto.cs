using System;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationApprovalDto
    {
        public Guid ApplicationId { get; set; }
        public Guid ApplicationStatusId { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public DateTime? DecisionDate { get; set; }
    }
}
