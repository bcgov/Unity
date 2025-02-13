using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Attachments;

public abstract class AbstractAttachmentBase : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public abstract AttachmentType AttachmentType { get; }
    
    public string? FileName { get; set; }

    [MaxLength(1024)]
    public string? DisplayName { get; set; }

    public Guid? TenantId { get; set; }
}
