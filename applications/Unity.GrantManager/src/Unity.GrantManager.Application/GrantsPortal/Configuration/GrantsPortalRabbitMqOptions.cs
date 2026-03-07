namespace Unity.GrantManager.GrantsPortal.Configuration;

public class GrantsPortalRabbitMqOptions
{
    public const string SectionName = "RabbitMQ:GrantsPortal";

    /// <summary>
    /// The integration source identifier used in the IntegrationMessages table.
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
    public int MessageRetentionDays { get; set; } = 30;
}
