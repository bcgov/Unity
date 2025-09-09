using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public static class QueueingStartupExtensions
    {
        public static void ConfigureRabbitMQ(this IServiceCollection services)
        {
            var configuration = services.GetConfiguration();
            services.TryAddSingleton<IAsyncConnectionFactory>(provider =>
            {
                var factory = new ConnectionFactory
                {
                    UserName = configuration.GetValue<string>("RabbitMQ:UserName") ?? "",
                    Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "",
                    HostName = configuration.GetValue<string>("RabbitMQ:HostName") ?? "",
                    VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/",
                    Port = configuration.GetValue<int>("RabbitMQ:Port"),
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    // Configure the amount of concurrent consumers within one host
                    ConsumerDispatchConcurrency = QueueingConstants.MAX_RABBIT_CONCURRENT_CONSUMERS,
                };
                return factory;
            });

            services.TryAddSingleton<IConnectionProvider, ConnectionProvider>();
            services.TryAddScoped<IChannelProvider, ChannelProvider>();

            services.TryAddScoped(typeof(IQueueChannelProvider<>), typeof(QueueChannelProvider<>));
            services.TryAddScoped(typeof(IQueueProducer<>), typeof(QueueProducer<>));
        }

        public static void AddQueueMessageConsumer<TMessageConsumer, TQueueMessage>(this IServiceCollection services) where TMessageConsumer : IQueueConsumer<TQueueMessage> where TQueueMessage : class, IQueueMessage
        {
            services.AddScoped(typeof(TMessageConsumer));
            services.AddScoped<IQueueConsumerHandler<TMessageConsumer, TQueueMessage>, QueueConsumerHandler<TMessageConsumer, TQueueMessage>>();
            services.AddHostedService<QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>>();
        }
    }
}
