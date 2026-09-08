using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace Unity.TenantManagement;

public class TenantDto : ExtensibleEntityDto<Guid>, IHasConcurrencyStamp
{
    public string Name { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CasClientCode { get; set; } = string.Empty;
    public string LicencePlate { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized list of <c>PostTenantCreationStepStatusEntry</c> (post-tenant-creation
    /// step statuses, e.g. Metabase sync) - kept as raw JSON rather than a typed list so this
    /// contracts project doesn't need a reference to the shared kernel project that owns the
    /// type; the Tenants UI parses it client-side.
    /// </summary>
    public string Sections { get; set; } = "[]";

    public string ConcurrencyStamp { get; set; }
}
