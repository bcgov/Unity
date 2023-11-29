using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationSubSector : AuditedAggregateRoot<Guid>
{
    public string SubSectorName { get; set; } = string.Empty;

    public string SubSectorCode { get; set; } = string.Empty;

    public Guid SectorId { get; set; }

    public ApplicationSector Sector { get; set; } = null!;
}
