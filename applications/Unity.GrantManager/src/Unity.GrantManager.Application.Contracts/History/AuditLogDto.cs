using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace Unity.GrantManager.History;

public class AuditLogDto : EntityDto<Guid>
{
    public Guid AuditLogId { get; set; }
    public DateTime ChangeTime { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string EntityTypeFullName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityUrl { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string OriginalValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public EntityChangeType ChangeType { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserFirstName { get; set; } = string.Empty;
    public string UserSurname { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
}