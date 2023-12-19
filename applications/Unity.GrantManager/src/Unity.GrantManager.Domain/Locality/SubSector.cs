using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Locality;

public class SubSector : AuditedAggregateRoot<Guid>
{
    public string SubSectorName { get; set; } = string.Empty;

    public string SubSectorCode { get; set; } = string.Empty;

    public Guid SectorId { get; set; }

    public Sector Sector { get; set; } = null!;
}
