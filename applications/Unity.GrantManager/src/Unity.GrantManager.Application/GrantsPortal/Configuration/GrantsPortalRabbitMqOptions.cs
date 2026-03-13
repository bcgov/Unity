namespace Unity.GrantManager.GrantsPortal.Configuration;

public class GrantsPortalRabbitMqOptions
{
    public const string SectionName = "RabbitMQ:GrantsPortal";

    /// <summary>
    /// The integration source identifier used for Grants Portal inbox/outbox messages..
    /// </summary>
    public const string SourceName = "GrantsPortal";

    public string Exchange { get; set; } = "grants.messaging";
    public string ExchangeType { get; set; } = "topic";
    public string InboundQueue { get; set; } = "unity.commands";
    public string[] InboundRoutingKeys { get; set; } = ["commands.unity.plugindata"];
    public string AckRoutingKey { get; set; } = "grants.unity.acknowledgment";

    /// <summary>
    /// Number of days to retain processed/failed messages before cleanup.
    /// </summary>
    public int MessageRetentionDays { get; set; } = 7;

    /// <summary>
    /// Cron expression for the inbox processor worker. Default: every 5 seconds.
    /// </summary>
    public string InboxProcessorCron { get; set; } = "0/5 * * * * ?";

    /// <summary>
    /// Cron expression for the outbox processor worker. Default: every 5 seconds.
    /// </summary>
    public string OutboxProcessorCron { get; set; } = "0/5 * * * * ?";

    /// <summary>
    /// Cron expression for the message cleanup worker. Default: once a day at midnight.
    /// </summary>
    public string MessageCleanupCron { get; set; } = "0 0 0 * * ?";
}
