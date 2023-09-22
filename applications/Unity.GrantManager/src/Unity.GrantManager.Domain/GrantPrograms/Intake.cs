using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.GrantPrograms;

public class Intake : FullAuditedAggregateRoot<Guid>
{
    public Double Budget { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string IntakeName { get; set; } = string.Empty;
}
