using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class PooledQueueChannelProvider<TQueueMessage>(IChannelProvider channelProvider,
                                      ILogger<PooledQueueChannelProvider<TQueueMessage>> logger) : IQueueChannelProvider<TQueueMessage>, IDisposable
        where TQueueMessage : IQueueMessage
    {
        private readonly IChannelProvider _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
        private readonly ILogger<PooledQueueChannelProvider<TQueueMessage>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ConcurrentQueue<IModel> _channelPool = new();
        private readonly SemaphoreSlim _channelSemaphore = new(MaxChannels, MaxChannels);
        private readonly string _queueName = typeof(TQueueMessage).Name;
        private bool _disposed;
        private const int MaxChannels = 10000;

        /// <summary>
        /// Get a channel from the pool or create a new one
        /// </summary>
        public IModel GetChannel()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(PooledQueueChannelProvider<TQueueMessage>));

            _channelSemaphore.Wait(); // Wait for an available slot

            try
            {
                while (_channelPool.TryDequeue(out var pooledChannel))
                {
                    if (pooledChannel.IsOpen)
                        return pooledChannel;

                    DisposeChannel(pooledChannel);
                }

                // No available channel, create a new one
                var channel = _channelProvider.GetChannel() ?? throw new InvalidOperationException("Channel cannot be null.");
                DeclareQueueAndDeadLetter(channel);
                return channel;
            }
            catch
            {
                _channelSemaphore.Release(); // Release if failed
                throw;
            }
        }

        /// <summary>
        /// Return a channel to the pool
        /// </summary>
        public void ReturnChannel(IModel channel)
        {
            if (channel != null && !_disposed && channel.IsOpen)
                _channelPool.Enqueue(channel);
            else if (channel != null)
                DisposeChannel(channel);

            _channelSemaphore.Release();
        }

        private void DeclareQueueAndDeadLetter(IModel channel)
        {
            try
            {
                try
                {
                    channel.QueueDeclarePassive(_queueName);
                    DeclareCompatibleQueue(channel);
                }
                catch (global::RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
                {
                    if (ex.ShutdownReason.ReplyCode == 404)
                        DeclareQueueWithDeadLetter(channel);
                    else if (ex.ShutdownReason.ReplyText.Contains("inequivalent arg"))
                    {
                        _logger.LogDebug(ex, "Queue {QueueName} exists with incompatible configuration, running compatibility mode.", _queueName);
                        DeclareCompatibleQueue(channel);
                    }
                    else
                        throw;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to declare queues for {_queueName}", ex);
            }
        }

        private void DeclareQueueWithDeadLetter(IModel channel)
        {
            var dlxName = $"{_queueName}.dlx";
            var dlqName = $"{_queueName}{QueueingConstants.DeadletterAddition}";
            var mainExchange = $"{_queueName}.exchange";

            channel.ExchangeDeclare(dlxName, ExchangeType.Direct, durable: true);

            var dlqArgs = new Dictionary<string, object>
            {
                { "x-queue-type", "quorum" },
                { "x-overflow", "reject-publish" }
            };

            channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false, arguments: dlqArgs);
            channel.QueueBind(dlqName, dlxName, dlqName);

            channel.ExchangeDeclare(mainExchange, ExchangeType.Direct, durable: true);

            var mainQArgs = new Dictionary<string, object>
            {
                { "x-queue-type", "quorum" },
                { "x-overflow", "reject-publish" },
                { "x-dead-letter-exchange", dlxName },
                { "x-dead-letter-routing-key", dlqName },
                { "x-dead-letter-strategy", "at-least-once" },
                { "x-delivery-limit", 10 }
            };

            channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: mainQArgs);
            channel.QueueBind(_queueName, mainExchange, _queueName);
        }

        private void DeclareCompatibleQueue(IModel channel)
        {
            var mainExchange = $"{_queueName}.exchange";

            try
            {
                channel.ExchangeDeclare(mainExchange, ExchangeType.Direct, durable: true);
                channel.QueueBind(_queueName, mainExchange, _queueName);

                _logger.LogWarning("Queue {QueueName} exists with incompatible configuration. Running in compatibility mode.", _queueName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to declare queue {_queueName} in compatibility mode.", ex);
            }
        }

        private static void DisposeChannel(IModel channel)
        {
            if (channel == null) return;

            try { if (channel.IsOpen) channel.Close(); } catch { }
            try { channel.Dispose(); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            while (_channelPool.TryDequeue(out var channel))
            {
                DisposeChannel(channel);
            }

            _channelSemaphore.Dispose();

            // Prevent the GC from calling a finalizer
            GC.SuppressFinalize(this);
        }

        public string QueueName => _queueName;
    }
}
