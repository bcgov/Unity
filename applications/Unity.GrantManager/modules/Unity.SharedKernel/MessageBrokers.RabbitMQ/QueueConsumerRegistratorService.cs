using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public class QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage> : IHostedService where TMessageConsumer : IQueueConsumer<TQueueMessage> where TQueueMessage : class, IQueueMessage
    {
        private readonly ILogger<QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>> _logger;
        private IQueueConsumerHandler<TMessageConsumer, TQueueMessage>? _consumerHandler;
        private readonly IServiceProvider _serviceProvider;
        private IServiceScope? _scope;

        public QueueConsumerRegistratorService(ILogger<QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(5);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Registering consumer {ConsumerName} (attempt {Attempt}/{MaxRetries})",
                        typeof(TMessageConsumer).Name, attempt, maxRetries);

                    _scope = _serviceProvider.CreateScope();
                    _consumerHandler = _scope.ServiceProvider.GetRequiredService<IQueueConsumerHandler<TMessageConsumer, TQueueMessage>>();
                    _consumerHandler.RegisterQueueConsumer();

                    _logger.LogInformation("Successfully registered consumer {ConsumerName}", typeof(TMessageConsumer).Name);
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Failed to register consumer {ConsumerName} on attempt {Attempt}. Retrying in {Delay}s...",
                        typeof(TMessageConsumer).Name, attempt, retryDelay.TotalSeconds);

                    _scope?.Dispose();
                    _scope = null;

                    await Task.Delay(retryDelay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register consumer {ConsumerName} after {MaxRetries} attempts",
                        typeof(TMessageConsumer).Name, maxRetries);

                    _scope?.Dispose();
                    _scope = null;
                    throw;
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var QueueServiceName = nameof(QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>);
            var ConsumerName = typeof(TMessageConsumer).Name;
            var MessageName = typeof(TQueueMessage).Name;

            _logger.LogInformation("Stop {QueueServiceName}: Cancelling {ConsumerName} as Consumer for Queue {MessageName}", QueueServiceName, ConsumerName, MessageName);

            try
            {
                _consumerHandler?.CancelQueueConsumer();
                _scope?.Dispose();
            }
            catch (Exception ex)
            {
                var ExceptionMessage = ex.Message;
                _logger.LogError(ex, "QueueConsumerRegistratorService StopAsync Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return Task.CompletedTask;
        }
    }

}
