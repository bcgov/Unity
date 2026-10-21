using System;
using Volo.Abp.Auditing;

namespace Unity.GrantManager.Applications;

public class AuditLogEntityChange
{
    public Guid AuditLogId { get; set; }
    public Guid EntityChangeId { get; set; }
    public DateTime ChangeTime { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string EntityTypeFullName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
    public EntityChangeType ChangeType { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserFirstName { get; set; }
    public string? UserSurname { get; set; }
    public string? TenantName { get; set; }
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public string? Url { get; set; }
    public string? HttpMethod { get; set; }
    public int? HttpStatusCode { get; set; }
}