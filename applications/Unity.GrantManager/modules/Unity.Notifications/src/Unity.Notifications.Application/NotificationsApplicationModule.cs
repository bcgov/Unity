using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Unity.Notifications.Integrations.Ches;
using Volo.Abp.BackgroundJobs;
using Unity.Notifications.EmailNotifications;
using Microsoft.Extensions.Configuration;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using RabbitMQ.Client;
using Unity.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Notifications.Integrations.RabbitMQ.QueueMessages;
using Unity.Shared.MessageBrokers.RabbitMQ;
using Unity.Notifications.Integrations.RabbitMQ;

namespace Unity.Notifications;

[DependsOn(
    typeof(NotificationsDomainModule),
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpBackgroundJobsModule),
    typeof(AbpBackgroundWorkersQuartzModule)
    )]
[DependsOn(typeof(AbpBackgroundWorkersQuartzModule))]
public class NotificationsApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddAutoMapperObjectMapper<NotificationsApplicationModule>();
        context.Services.AddScoped<IChesClientService, ChesClientService>();

        Configure<EmailBackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
            options.EmailResend.RetryAttemptsMaximum = configuration.GetValue<int>("BackgroundJobs:EmailResend:RetryAttemptsMaximum");
        });

        context.Services.AddSingleton<IAsyncConnectionFactory>(provider =>
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

        context.Services.AddSingleton<IConnectionProvider, ConnectionProvider>();
        context.Services.AddScoped<IChannelProvider, ChannelProvider>();
        context.Services.AddScoped(typeof(IQueueChannelProvider<>), typeof(QueueChannelProvider<>));
        context.Services.AddScoped(typeof(IQueueProducer<>), typeof(QueueProducer<>));
        context.Services.AddQueueMessageConsumer<EmailConsumer, EmailMessages>();

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });
    }
}