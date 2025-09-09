using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;

namespace Unity.Modules.Shared.MessageBrokers.RabbitMQ
{
    public static class QueueingStartupExtensions
    {
        public static IServiceCollection ConfigureRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RabbitMQOptions>()
                .Bind(configuration.GetSection(RabbitMQOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // Register connection factory
            services.TryAddSingleton<IConnectionFactory>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<RabbitMQOptions>>().Value;

                return new ConnectionFactory
                {
                    UserName = options.UserName,
                    Password = options.Password,
                    HostName = options.HostName,
                    VirtualHost = options.VirtualHost,
                    Port = options.Port,
                    DispatchConsumersAsync = options.DispatchConsumersAsync,
                    AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                    ConsumerDispatchConcurrency = options.ConsumerDispatchConcurrency,
                    NetworkRecoveryInterval = options.NetworkRecoveryInterval,
                    RequestedConnectionTimeout = options.RequestedConnectionTimeout,
                    RequestedHeartbeat = options.RequestedHeartbeat,
                };
            });

            // Core services
            services.TryAddSingleton<IConnectionProvider, ConnectionProvider>();
            services.TryAddScoped<IChannelProvider, ChannelProvider>();

            // Queue services
            services.TryAddScoped(typeof(IQueueChannelProvider<>), typeof(QueueChannelProvider<>));
            services.TryAddScoped(typeof(IQueueProducer<>), typeof(QueueProducer<>));

            return services;
        }

        public static IServiceCollection AddQueueMessageConsumer<TMessageConsumer, TQueueMessage>(
            this IServiceCollection services)
            where TMessageConsumer : class, IQueueConsumer<TQueueMessage>
            where TQueueMessage : class, IQueueMessage
        {
            services.AddScoped<TMessageConsumer>();
            services.AddScoped<IQueueConsumerHandler<TMessageConsumer, TQueueMessage>,
                QueueConsumerHandler<TMessageConsumer, TQueueMessage>>();
            services.AddHostedService<QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>>();

            return services;
        }

        /// <summary>
        /// Adds multiple message consumers at once.
        /// </summary>
        public static IServiceCollection AddQueueMessageConsumers(
            this IServiceCollection services,
            params Type[] consumerTypes)
        {
            foreach (var consumerType in consumerTypes)
            {
                if (!consumerType.IsClass || consumerType.IsAbstract)
                {
                    throw new ArgumentException($"Consumer type {consumerType.Name} must be a concrete class");
                }

                var queueConsumerInterface = consumerType.GetInterface("IQueueConsumer`1")
                    ?? throw new ArgumentException(
                        $"Consumer type {consumerType.Name} must implement IQueueConsumer<T>");

                var messageType = queueConsumerInterface.GetGenericArguments()[0];

                // Register the consumer type as scoped
                services.AddScoped(consumerType);

                // Register IQueueConsumerHandler<TMessageConsumer, TQueueMessage>
                var handlerInterfaceType = typeof(IQueueConsumerHandler<,>).MakeGenericType(consumerType, messageType);
                var handlerImplType = typeof(QueueConsumerHandler<,>).MakeGenericType(consumerType, messageType);
                services.AddScoped(handlerInterfaceType, handlerImplType);

                // Register the hosted service for the consumer
                var registratorType = typeof(QueueConsumerRegistratorService<,>).MakeGenericType(consumerType, messageType);
                services.AddSingleton(typeof(Microsoft.Extensions.Hosting.IHostedService), registratorType);
            }

            return services;
        }
    }
}
