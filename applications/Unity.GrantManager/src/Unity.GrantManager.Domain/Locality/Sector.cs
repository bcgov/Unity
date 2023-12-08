using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locality;

public class Sector : AuditedAggregateRoot<Guid>
{
    public string SectorName { get; set; } = string.Empty;

    public string SectorCode { get; set; } = string.Empty;

    public ICollection<SubSector> SubSectors { get; set; } = new List<SubSector>();
}
