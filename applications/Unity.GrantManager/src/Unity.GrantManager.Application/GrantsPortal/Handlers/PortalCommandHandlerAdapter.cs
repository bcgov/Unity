using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.GrantManager.GrantsPortal.Configuration;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.Messaging;

namespace Unity.GrantManager.GrantsPortal.Handlers;

/// <summary>
/// Adapts the portal-specific <see cref="IPortalCommandHandler"/> to the generic
/// <see cref="IInboxMessageHandler"/> interface so existing handlers don't need to change.
///
/// Each <see cref="IPortalCommandHandler"/> is wrapped in one of these adapters at DI registration time.
/// The adapter handles PluginDataEnvelope → PluginDataPayload deserialization before delegating.
/// </summary>
internal class PortalCommandHandlerAdapter(IPortalCommandHandler inner) : IInboxMessageHandler
{
    public string Source => GrantsPortalRabbitMqOptions.SourceName;
    public string DataType => inner.DataType;

    public async Task<string> HandleAsync(string rawPayload)
    {
        var envelope = JsonConvert.DeserializeObject<PluginDataEnvelope>(rawPayload)
                       ?? throw new JsonException("Failed to deserialize message payload");

        var payload = envelope.Data?.ToObject<PluginDataPayload>()
                      ?? throw new ArgumentException("Message data payload is missing");

        return await inner.HandleAsync(payload);
    }
}
