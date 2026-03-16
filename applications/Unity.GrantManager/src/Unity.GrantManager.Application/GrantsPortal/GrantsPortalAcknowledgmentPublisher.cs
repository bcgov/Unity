using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RabbitMQ.Client;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.GrantsPortal.Messages;

namespace Unity.GrantManager.GrantsPortal;

public class GrantsPortalAcknowledgmentPublisher(
    IOptions<GrantsPortalRabbitMqOptions> options,
    ILogger<GrantsPortalAcknowledgmentPublisher> logger)
{
    private readonly GrantsPortalRabbitMqOptions _options = options.Value;
    private static readonly JsonSerializerSettings s_jsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };

    public void Publish(IModel channel, string originalMessageId, string correlationId, string status, string details)
    {
        var ack = new MessageAcknowledgment
        {
            MessageId = Guid.NewGuid().ToString(),
            OriginalMessageId = originalMessageId,
            CorrelationId = correlationId,
            Status = status,
            Details = details,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };

        var json = JsonConvert.SerializeObject(ack, s_jsonSettings);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Type = "MessageAcknowledgment";
        properties.ContentType = "application/json";
        properties.ContentEncoding = "utf-8";
        properties.Persistent = true;
        properties.MessageId = ack.MessageId;
        properties.CorrelationId = correlationId;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: _options.Exchange,
            routingKey: _options.AckRoutingKey,
            basicProperties: properties,
            body: body);

        logger.LogInformation(
            "Published {Status} acknowledgment for message {OriginalMessageId} with ack id {AckMessageId}",
            status, originalMessageId, ack.MessageId);
    }
}
