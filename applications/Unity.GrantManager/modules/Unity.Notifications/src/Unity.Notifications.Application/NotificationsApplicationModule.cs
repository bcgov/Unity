using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Unity.Notifications.Integrations.Ches;
using Unity.Notifications.EmailNotifications;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundJobs.RabbitMQ;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.RabbitMQ;


namespace Unity.Notifications;

[DependsOn(
    typeof(NotificationsDomainModule),
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpBackgroundJobsModule)
    )]
public class NotificationsApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<NotificationsApplicationModule>();
        context.Services.AddScoped<IChesClientService, ChesClientService>();
        
        Configure<AbpRabbitMqOptions>(options =>
        {
            options.Connections.Default.UserName = "guest";
            options.Connections.Default.Password = "guest";
            options.Connections.Default.HostName = "localhost";
            options.Connections.Default.Port = 5672;
            options.Connections.Default.VirtualHost = "/";
        });

        Configure<AbpRabbitMqBackgroundJobOptions>(options =>
        {
            options.DefaultQueueNamePrefix = "unity_jobs.";
            options.DefaultDelayedQueueNamePrefix = "unity_jobs.delayed";
            options.PrefetchCount = 1;
            options.JobQueues[typeof(EmailSendingArgs)] =
                new JobQueueConfiguration(
                    typeof(EmailSendingArgs),
                    queueName: "unity_jobs.emails",
                    connectionName: "Default",
                    delayedQueueName:"unity_jobs.emails.delayed"
                );
        });

        Configure<AbpRabbitMqEventBusOptions>(options =>
        {
            options.ExchangeArguments["x-delayed-type"] = "direct";
            //options.QueueArguments["x-message-ttl"] = 60000;
        });
    }
}