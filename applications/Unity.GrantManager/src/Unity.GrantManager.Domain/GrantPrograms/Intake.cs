using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.GrantPrograms;

public class Intake : FullAuditedAggregateRoot<Guid>
{
    public double Budget { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
    
    public string IntakeName { get; set; } = string.Empty;
}
