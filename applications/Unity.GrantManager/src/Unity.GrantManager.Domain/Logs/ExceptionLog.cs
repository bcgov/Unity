using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Logs;

public class ExceptionLog : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? TenantName { get; set; }
    public ExceptionLogType NotificationType { get; set; }
    public ExceptionLogChannel Channel { get; set; }
    public ExceptionLogSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? SourceReference { get; set; }
    public string? PayloadJson { get; set; }
    public string? CorrelationId { get; set; }
    public int OccurrenceCount { get; set; } = 1;
    public bool IsDeliveredRealtime { get; set; }
    public string? DeliveryTarget { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackExcerpt { get; set; }
    public string? SourceFile { get; set; }
    public int? SourceLine { get; set; }
    public string? CommitSha { get; set; }
    public string? Environment { get; set; }

    // Git blame enrichment: identifies who wrote the failing line and which PR/ticket shipped it.
    public string? BlameAuthor { get; set; }
    public string? BlameEmail { get; set; }
    public string? BlameCommitSha { get; set; }
    public string? BlameCommitMessage { get; set; }
    public string? PullRequestUrl { get; set; }
    public int? PullRequestNumber { get; set; }
    public string? PullRequestTitle { get; set; }
    public string? TicketReference { get; set; }
}
