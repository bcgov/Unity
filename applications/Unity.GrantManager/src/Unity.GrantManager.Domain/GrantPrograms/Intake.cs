using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.GrantPrograms;

public class Intake : FullAuditedAggregateRoot<Guid>
{
    public Double Budget { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
    
    public string IntakeName { get; set; }
}
