using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unity.RabbitMQ.Interfaces;

namespace Unity.RabbitMQ
{
    public class QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage> : IHostedService where TMessageConsumer : IQueueConsumer<TQueueMessage> where TQueueMessage : class, IQueueMessage
    {
        private readonly ILogger<QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>> _logger;
        private IQueueConsumerHandler<TMessageConsumer, TQueueMessage> _consumerHandler;
        private readonly IServiceProvider _serviceProvider;
        private IServiceScope _scope;

        public QueueConsumerRegistratorService(ILogger<QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Registering {typeof(TMessageConsumer).Name} as Consumer for Queue {typeof(TQueueMessage).Name}");
            try
            {
                // Every registration of a IQueueConsumerHandler will have it's own scope
                // This will result in messages to the QeueueConsumer will have their own incomming RabbitMQ channel
                _scope = _serviceProvider.CreateScope();

                _consumerHandler = _scope.ServiceProvider.GetRequiredService<IQueueConsumerHandler<TMessageConsumer, TQueueMessage>>();
                _consumerHandler.RegisterQueueConsumer();
            }
            catch (Exception ex)
            {
                var MessageException = ex.Message;
                _logger.LogError(ex, "QueueConsumerRegistratorService StartAsync {MessageException}", MessageException);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stop {nameof(QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>)}: Canceling {typeof(TMessageConsumer).Name} as Consumer for Queue {typeof(TQueueMessage).Name}");
            try
            {
                _consumerHandler.CancelQueueConsumer();
                _scope.Dispose();
            } catch (Exception ex) {
                var ExceptionMessage = ex.Message;
                _logger.LogError(ex, "QueueConsumerRegistratorService StopAsync Exception: {ExceptionMessage}", ExceptionMessage);
            }
            return Task.CompletedTask;
        }
    }

}
