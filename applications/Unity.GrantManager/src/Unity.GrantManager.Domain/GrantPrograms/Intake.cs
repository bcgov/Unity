using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.GrantPrograms;

public class Intake : FullAuditedAggregateRoot<Guid>
{
    public Guid GrantProgramId { get; set; }

    public string IntakeName { get; set; }
}
