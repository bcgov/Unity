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
            // Configure options with validation
            services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.SectionName));
            services.AddOptions<RabbitMQOptions>()
                .Bind(configuration.GetSection(RabbitMQOptions.SectionName))
                .ValidateDataAnnotations();

            // Register connection factory
            services.TryAddSingleton<IAsyncConnectionFactory>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<RabbitMQOptions>>().Value;

                var factory = new ConnectionFactory
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

                return factory;
            });

            // Register core services
            services.TryAddSingleton<IConnectionProvider, ConnectionProvider>();
            services.TryAddScoped<IChannelProvider, ChannelProvider>();

            // Register queue services
            services.TryAddScoped(typeof(IQueueChannelProvider<>), typeof(QueueChannelProvider<>));
            services.TryAddScoped(typeof(IQueueProducer<>), typeof(QueueProducer<>));

            return services;
        }

        /// <summary>
        /// Alternative method that works with service provider
        /// </summary>
        public static IServiceCollection ConfigureRabbitMQ(this IServiceCollection services)
        {
            services.TryAddSingleton<IAsyncConnectionFactory>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var options = new RabbitMQOptions(configuration);
                configuration.GetSection(RabbitMQOptions.SectionName).Bind(options);

                // Validate configuration
                // If you need custom validation, implement it here or rely on data annotations.
                var factory = new ConnectionFactory
                {
                    UserName = configuration.GetValue<string>("RabbitMQ:UserName") ?? "",
                    Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "",
                    HostName = configuration.GetValue<string>("RabbitMQ:HostName") ?? "",
                    VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "/",
                    Port = configuration.GetValue<int>("RabbitMQ:Port"),
                    DispatchConsumersAsync = options.DispatchConsumersAsync,
                    AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                    ConsumerDispatchConcurrency = options.ConsumerDispatchConcurrency,
                    NetworkRecoveryInterval = options.NetworkRecoveryInterval,
                    RequestedConnectionTimeout = options.RequestedConnectionTimeout,
                    RequestedHeartbeat = options.RequestedHeartbeat,
                };

                return factory;
            });

            services.TryAddSingleton<IConnectionProvider, ConnectionProvider>();
            services.TryAddScoped<IChannelProvider, ChannelProvider>();
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
            services.AddScoped<IQueueConsumerHandler<TMessageConsumer, TQueueMessage>, QueueConsumerHandler<TMessageConsumer, TQueueMessage>>();
            services.AddHostedService<QueueConsumerRegistratorService<TMessageConsumer, TQueueMessage>>();

            return services;
        }

        /// <summary>
        /// Adds multiple message consumers at once
        /// </summary>
        public static IServiceCollection AddQueueMessageConsumers(
            this IServiceCollection services,
            params Type[] consumerTypes)
        {
            foreach (var consumerType in consumerTypes)
            {
                if (!consumerType.IsClass || consumerType.IsAbstract)
                    throw new ArgumentException($"Consumer type {consumerType.Name} must be a concrete class");

                var queueConsumerInterface = consumerType.GetInterface("IQueueConsumer`1") ?? throw new ArgumentException($"Consumer type {consumerType.Name} must implement IQueueConsumer<T>");
                var messageType = queueConsumerInterface.GetGenericArguments()[0];

                var addConsumerMethod = typeof(QueueingStartupExtensions)
                    .GetMethod(nameof(AddQueueMessageConsumer))
                    ?.MakeGenericMethod(consumerType, messageType);

                addConsumerMethod?.Invoke(null, [services]);
            }

            return services;
        }
    }
}