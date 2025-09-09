using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.GrantManager
{
    public class FakeChannelProvider : IChannelProvider
    {
        public IModel GetChannel() => new FakeModel();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void ReturnChannel(IModel channel) { }

        public class FakeModel : IModel
        {
            public int ChannelNumber => 1;
            public ShutdownEventArgs? CloseReason => null;
            public IBasicConsumer? DefaultConsumer { get; set; }
            public bool IsClosed => false;
            public bool IsOpen => true;
            public ulong NextPublishSeqNo => 0;
            public string CurrentQueue => string.Empty;
            public TimeSpan ContinuationTimeout { get; set; } = TimeSpan.Zero;

            // Events (no-op) - this section is needed and unused
            #pragma warning disable S1144 // Unused private code
            public event EventHandler<BasicAckEventArgs>? BasicAcks;
            
            public event EventHandler<BasicNackEventArgs>? BasicNacks;
            public event EventHandler<EventArgs>? BasicRecoverOk;
            public event EventHandler<BasicReturnEventArgs>? BasicReturn;
            public event EventHandler<CallbackExceptionEventArgs>? CallbackException;
            public event EventHandler<FlowControlEventArgs>? FlowControl;
            public event EventHandler<ShutdownEventArgs>? ModelShutdown;
            #pragma warning restore S1144

            // --- Minimal stubs for queue + exchange setup ---
            public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
                => new(queue, 0, 0);

            public QueueDeclareOk QueueDeclarePassive(string queue)
                => new(queue, 0, 0);

            public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments) { }

            public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments) { }
            public void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments) { }
            public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments) { }

            public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments) { }
            public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments) { }
            public void ExchangeDeclarePassive(string exchange) { }

            public void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments) { }
            public void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments) { }
            public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments) { }
            public void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments) { }

            public void ExchangeDelete(string exchange, bool ifUnused) { }
            public void ExchangeDeleteNoWait(string exchange, bool ifUnused) { }

            // --- Commonly invoked but safe to no-op in tests ---
            public void BasicAck(ulong deliveryTag, bool multiple) { }
            public void BasicNack(ulong deliveryTag, bool multiple, bool requeue) { }
            public void BasicReject(ulong deliveryTag, bool requeue) { }
            public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
                => Guid.NewGuid().ToString();
            public void BasicCancel(string consumerTag) { }
            public void BasicCancelNoWait(string consumerTag) { }
            public BasicGetResult BasicGet(string queue, bool autoAck) => new BasicGetResult(0, false, null, null, 0, null, null);
            public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body) { }
            public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global) { }

            public void ConfirmSelect() { }
            public IBasicPublishBatch CreateBasicPublishBatch() => new FakeBasicPublishBatch();
            public IBasicProperties CreateBasicProperties() => new FakeBasicProperties
            {
                AppId = string.Empty,
                ClusterId = string.Empty,
                ContentEncoding = string.Empty,
                ContentType = string.Empty,
                CorrelationId = string.Empty,
                Expiration = string.Empty,
                MessageId = string.Empty,
                ReplyTo = string.Empty,
                Type = string.Empty
            };

            public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty) => 0;
            public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty) { }
            public uint QueuePurge(string queue) => 0;

            public uint MessageCount(string queue) => 0;
            public uint ConsumerCount(string queue) => 0;

            public void TxSelect() { }
            public void TxCommit() { }
            public void TxRollback() { }

            public bool WaitForConfirms() => true;
            public bool WaitForConfirms(TimeSpan timeout) => true;
            public bool WaitForConfirms(TimeSpan timeout, out bool timedOut) { timedOut = false; return true; }
            public void WaitForConfirmsOrDie() { }
            public void WaitForConfirmsOrDie(TimeSpan timeout) { }

            public void BasicRecover(bool requeue) { }
            public void BasicRecoverAsync(bool requeue) { }

            // Lifecycle
            public void Abort() { }
            public void Abort(ushort replyCode, string replyText) { }
            public void Close() { }
            public void Close(ushort replyCode, string replyText) { }
            public void Dispose() => GC.SuppressFinalize(this);
        }

        // Fake BasicPublishBatch (so CreateBasicPublishBatch won’t explode if called)
        private class FakeBasicPublishBatch : IBasicPublishBatch
        {
            public void Add(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body) { }
            public static void Add(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body) { }
            public void Publish() { }
            public static void Clear() { }
        }

        // Fake BasicProperties (so BasicPublish won’t explode if called)
        private class FakeBasicProperties : IBasicProperties
        {
            public ushort ProtocolClassId => 60;
            public string ProtocolClassName => "basic";
            public void ClearAppId() { }
            public void ClearClusterId() { }
            public void ClearContentEncoding() { }
            public void ClearContentType() { }
            public void ClearCorrelationId() { }
            public void ClearDeliveryMode() { }
            public void ClearExpiration() { }
            public void ClearHeaders() { }
            public void ClearMessageId() { }
            public void ClearPriority() { }
            public void ClearReplyTo() { }
            public void ClearTimestamp() { }
            public void ClearType() { }
            public void ClearUserId() { }
            public bool IsAppIdPresent() => false;
            public bool IsClusterIdPresent() => false;
            public bool IsContentEncodingPresent() => false;
            public bool IsContentTypePresent() => false;
            public bool IsCorrelationIdPresent() => false;
            public bool IsDeliveryModePresent() => false;
            public bool IsExpirationPresent() => false;
            public bool IsHeadersPresent() => false;
            public bool IsMessageIdPresent() => false;
            public bool IsPriorityPresent() => false;
            public bool IsReplyToPresent() => false;
            public bool IsTimestampPresent() => false;
            public bool IsTypePresent() => false;
            public bool IsUserIdPresent() => false;

            // Safe defaults
            public required string AppId { get; set; }
            public required string ClusterId { get; set; }
            public required string ContentEncoding { get; set; }
            public required string ContentType { get; set; }
            public required string CorrelationId { get; set; }
            public byte DeliveryMode { get; set; }
            public required string Expiration { get; set; }
            public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
            public required string MessageId { get; set; }
            public byte Priority { get; set; }
            public required string ReplyTo { get; set; }
            public AmqpTimestamp Timestamp { get; set; }
            public required string Type { get; set; }
            public string? UserId { get; set; }
            public bool Persistent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public PublicationAddress ReplyToAddress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }

    }
}
