using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Messaging;

/// <summary>
/// An acknowledgment or response message to be published to an external system.
/// </summary>
public class OutboxMessage : AuditedAggregateRoot<Guid>, IMultiTenant
{
    /// <summary>
    /// Identifies the integration target (e.g. "GrantsPortal").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// A unique message ID for this outbound message.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The message ID of the original inbound command this is responding to.
    /// </summary>
    public string OriginalMessageId { get; set; } = string.Empty;

    /// <summary>
    /// The correlation ID passed through from the original inbound message.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The data type of the original command (e.g. CONTACT_CREATE_COMMAND).
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// The acknowledgment status: SUCCESS, FAILED, or PROCESSING.
    /// </summary>
    public string AckStatus { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable details (shown to the Portal user on failure).
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Current publish status.
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    /// <summary>
    /// Number of publish attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// When the outbox message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was successfully published to the broker.
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    public Guid? TenantId { get; set; }
}
