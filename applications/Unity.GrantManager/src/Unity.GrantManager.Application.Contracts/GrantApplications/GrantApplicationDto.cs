using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications
{
    public class GrantApplicationDto : AuditedEntityDto<Guid>
    {
        public string ProjectName { get; set; }
        public string ReferenceNo { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal EligibleAmount { get; set; }
        public List<GrantApplicationAssigneeDto> Assignees { get; set; }
        public DateTime SubmissionDate { get; set; }
        public GrantApplicationStatus Status { get; set; }
        public int Probability { get; set; }
        public DateTime ProposalDate { get; set; }
    }
}
