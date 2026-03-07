using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Messaging;

public enum MessageStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    Failed = 4
}

/// <summary>
/// A message received from an external system, stored for sequential processing.
/// </summary>
public class InboxMessage : AuditedAggregateRoot<Guid>, IMultiTenant
{
    /// <summary>
    /// Identifies the integration source (e.g. "GrantsPortal").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The message ID from the source system. Used for idempotency.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The correlation ID passed through from the source system.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// The command discriminator (e.g. CONTACT_CREATE_COMMAND).
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// The full JSON payload of the inbound message.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Current processing status.
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    /// <summary>
    /// Human-readable details (processing result or error message).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// When the message was received from the broker.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the message was successfully processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    public Guid? TenantId { get; set; }
}
