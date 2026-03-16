using System.Threading.Tasks;

namespace Unity.GrantManager.Messaging;

/// <summary>
/// A source-agnostic handler for inbox messages.
/// Implementations receive the raw JSON payload and are responsible for their own deserialization.
/// Dispatched by <see cref="InboxWorkerBase"/> based on <see cref="Source"/> and <see cref="DataType"/>.
/// </summary>
public interface IInboxMessageHandler
{
    /// <summary>
    /// The integration source this handler belongs to (e.g. "GrantsPortal").
    /// Must match the <see cref="InboxMessage.Source"/> value.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// The command discriminator this handler processes (e.g. "CONTACT_CREATE_COMMAND").
    /// Must match the <see cref="InboxMessage.DataType"/> value.
    /// </summary>
    string DataType { get; }

    /// <summary>
    /// Processes the raw JSON payload of an inbox message.
    /// </summary>
    /// <param name="rawPayload">The full JSON payload string from <see cref="InboxMessage.Payload"/>.</param>
    /// <returns>A human-readable details string describing the outcome.</returns>
    Task<string> HandleAsync(string rawPayload);
}
