using System;

namespace Unity.GrantManager.Logs;

public class ExceptionLogDto
{
    public Guid Id { get; set; }
    public DateTime CreationTime { get; set; }
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
    public int OccurrenceCount { get; set; }
    public string? CorrelationId { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackExcerpt { get; set; }
    public string? SourceFile { get; set; }
    public int? SourceLine { get; set; }
    public string? CommitSha { get; set; }
    public string? Environment { get; set; }
    public string? BlameAuthor { get; set; }
    public string? BlameEmail { get; set; }
    public string? BlameCommitSha { get; set; }
    public string? BlameCommitMessage { get; set; }
    public string? PullRequestUrl { get; set; }
    public int? PullRequestNumber { get; set; }
    public string? PullRequestTitle { get; set; }
    public string? TicketReference { get; set; }
}
