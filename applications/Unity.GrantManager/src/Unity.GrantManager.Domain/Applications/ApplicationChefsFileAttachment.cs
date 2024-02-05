using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationChefsFileAttachment : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public string? ChefsSumbissionId { get; set; }
    public string? ChefsFileId { get; set; }
    public string? Name { get; set; }
    public Guid? TenantId { get; set; }
}
