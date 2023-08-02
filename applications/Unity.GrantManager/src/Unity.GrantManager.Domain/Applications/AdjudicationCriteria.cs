using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public  class AdjudicationCriteria : AuditedAggregateRoot<Guid>
{
    public Guid IntakeId { get; set; }

    public string AdjudicationCriteriaName { get; set; }
}
