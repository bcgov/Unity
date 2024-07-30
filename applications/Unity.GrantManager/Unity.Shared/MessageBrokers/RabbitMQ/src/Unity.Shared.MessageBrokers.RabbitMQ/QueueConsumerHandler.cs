using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Unity.Shared.MessageBrokers.RabbitMQ.Exceptions;
using Unity.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Shared.MessageBrokers.RabbitMQ
{
    public class QueueConsumerHandler<TMessageConsumer, TQueueMessage> : IQueueConsumerHandler<TMessageConsumer, TQueueMessage> where TMessageConsumer : IQueueConsumer<TQueueMessage> where TQueueMessage : class, IQueueMessage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> _logger;
        private readonly string _queueName;
        private IModel? _consumerRegistrationChannel;
        private string? _consumerTag;
        private readonly string _consumerName;

        public QueueConsumerHandler(IServiceProvider serviceProvider,
            ILogger<QueueConsumerHandler<TMessageConsumer, TQueueMessage>> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            _queueName = typeof(TQueueMessage).Name;
            _consumerName = typeof(TMessageConsumer).Name;
        }

        public void RegisterQueueConsumer()
        {
            _logger.LogInformation("Registering {_consumerName} as a consumer for Queue {_queueName}", _consumerName, _queueName);

            var scope = _serviceProvider.CreateScope();

            // Request a channel for registering the Consumer for this Queue
            _consumerRegistrationChannel = scope.ServiceProvider.GetRequiredService<IQueueChannelProvider<TQueueMessage>>().GetChannel();
            if(_consumerRegistrationChannel != null )
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerRegistrationChannel);

                // Register the trigger
                consumer.Received += HandleMessage;
                try
                {
                    _consumerTag = _consumerRegistrationChannel.BasicConsume(_queueName, false, consumer);
                }
                catch (Exception ex)
                {
                    var RegisterExceptionMessage = $"BasicConsume failed for Queue '{_queueName}'";
                    _logger.LogError(ex, "QueueConsumerHandler - {RegisterExceptionMessage}", RegisterExceptionMessage);
                    throw new QueueingException(RegisterExceptionMessage);
                }

                _logger.LogInformation("Succesfully registered {_consumerName} as a Consumer for Queue {_queueName}", _consumerName, _queueName);
            }
        }

        public void CancelQueueConsumer()
        {
            if(_consumerRegistrationChannel != null) {
                _logger.LogInformation("Canceling QueueConsumer registration for {_consumerName}", _consumerName);
                try
                {
                    _consumerRegistrationChannel.BasicCancel(_consumerTag);
                }
                catch (Exception ex)
                {
                    var CancelExceptionMessage = $"Error canceling QueueConsumer registration for {_consumerName}";
                    _logger.LogError(ex, "QueueConsumerHandler Exception: {ExceptionMessage}", CancelExceptionMessage);
                    throw new QueueingException(CancelExceptionMessage, ex);
                }
            }
        }

        private async Task HandleMessage(object ch, BasicDeliverEventArgs ea)
        {
            _logger.LogInformation("Received Message on Queue {_queueName}", _queueName);

            // Create a new scope for handling the consumption of the queue message
            var consumerScope = _serviceProvider.CreateScope();

            // This is the channel on which the Queue message is delivered to the consumer
            var consumingChannel = ((AsyncEventingBasicConsumer)ch).Model;

            IModel? producingChannel = null;
            try
            {
                // Within this processing scope, we will open a new channel that will handle all messages produced within this consumer/scope.
                // This is neccessairy to be able to commit them as a transaction
                producingChannel = consumerScope.ServiceProvider.GetRequiredService<IChannelProvider>()
                    .GetChannel();

                // Serialize the message
                var message = DeserializeMessage(ea.Body.ToArray());
                if (producingChannel != null && message != null)
                {

                    var MessageId = message.MessageId;
                    _logger.LogInformation("MessageID '{MessageId}'", MessageId);

                    // Start a transaction which will contain all messages produced by this consumer
                    producingChannel.TxSelect();

                    // Request an instance of the consumer from the Service Provider
                    var consumerInstance = consumerScope.ServiceProvider.GetRequiredService<TMessageConsumer>();

                    // Trigger the consumer to start processing the message
                    await consumerInstance.ConsumeAsync(message);

                    // Ensure both channels are open before committing
                    if (producingChannel.IsClosed || consumingChannel.IsClosed)
                    {
                        throw new QueueingException("A channel is closed during processing");
                    }

                    // Commit the transaction of any messages produced within this consumer scope
                    producingChannel.TxCommit();

                    // Acknowledge successfull handling of the message
                    consumingChannel.BasicAck(ea.DeliveryTag, false);

                    _logger.LogInformation("Message succesfully processed");
                } else
                {
                    _logger.LogError("RegisterQueueConsumer: The Producer Channel was null");
                }
            }
            catch (Exception ex)
            {
                var HandleMessageException = $"Cannot handle consumption of a {_queueName} by {_consumerName}'";
                _logger.LogError(ex, "QueueConsumerHandler Exception: {ExceptionMessage}", HandleMessageException);
                if(producingChannel != null)
                {
                    RejectMessage(ea.DeliveryTag, consumingChannel, producingChannel);
                }                
            }
            finally
            {
                // Dispose the scope which ensures that all Channels that are created within the consumption process will be disposed
                consumerScope.Dispose();
            }
        }

        private void RejectMessage(ulong deliveryTag, IModel consumeChannel, IModel scopeChannel)
        {
            try
            {
                // The consumption process could fail before the scope channel is created
                if (scopeChannel != null)
                {
                    // Rollback any massages within the transaction
                    scopeChannel.TxRollback();
                    _logger.LogInformation("Rollbacked the transaction");
                }

                // Reject the message on the consumption channel
                consumeChannel.BasicReject(deliveryTag, false);

                _logger.LogWarning("Rejected queue message");
            }
            catch (Exception bex)
            {
                var ExceptionMessage = $"BasicReject failed";
                _logger.LogError(bex, "QueueConsumerHandler Exception: {ExceptionMessage}", ExceptionMessage);
            }
        }

        private static TQueueMessage? DeserializeMessage(byte[] message)
        {
            var stringMessage = Encoding.UTF8.GetString(message);
            return JsonConvert.DeserializeObject<TQueueMessage>(stringMessage);
        }
    }

}
