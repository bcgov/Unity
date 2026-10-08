using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace Unity.GrantManager.History;

public class GetAuditLogsInput : PagedAndSortedResultRequestDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? EntityTypeFullName { get; set; }
    public EntityChangeType? ChangeType { get; set; }
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public string? Filter { get; set; }
}