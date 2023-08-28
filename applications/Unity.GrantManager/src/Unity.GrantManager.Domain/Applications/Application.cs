using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class Application : AuditedAggregateRoot<Guid>
{
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationStatusId { get; set; }
    public string ProjectName { get; set; }
    public string ReferenceNo { get; set; }
    public Double EligibleAmount { get; set; }
    public Double RequestedAmount { get; set; }
    public DateTime ProposalDate { get; set; }
    public DateTime SubmissionDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Payload { get; set; }    
                     
                       
}
