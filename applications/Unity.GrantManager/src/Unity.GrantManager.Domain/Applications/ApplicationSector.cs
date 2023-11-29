using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationSector : AuditedAggregateRoot<Guid>
{
    public string SectorName { get; set; } = string.Empty;

    public string SectorCode { get; set; } = string.Empty;

    public ICollection<ApplicationSubSector> SubSectors { get; } = new List<ApplicationSubSector>();
}
