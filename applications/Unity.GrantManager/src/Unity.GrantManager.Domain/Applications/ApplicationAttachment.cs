﻿using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationAttachment : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationId { get; set; }
    public string S3ObjectKey { get; set; } = String.Empty;
    public Guid UserId { get; set; }
    public string? FileName { get; set; }
    public DateTime Time { get; set; }
    public Guid? TenantId { get; set; }
}
