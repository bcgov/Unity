using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class AdjudicationAssessment : AuditedAggregateRoot<Guid>
{
    public string OidcSub { get; set; }
    public Guid ApplicationFormId { get; set; }

    public Guid ApplicantId { get; set; }

    public string ChefsSubmissionGuid { get; set; }
}
