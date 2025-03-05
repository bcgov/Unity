using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.MultiTenancy;
using Unity.Notifications.Integrations.RabbitMQ.QueueMessages;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ;
using Unity.Notifications.Integrations.RabbitMQ;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Http.Client;
using Unity.Modules.Shared.Http;

namespace Unity.Notifications;

[DependsOn(
    typeof(NotificationsDomainModule),
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpBackgroundJobsModule),
    typeof(AbpBackgroundWorkersQuartzModule),
    typeof(AbpHttpClientModule)
    )]
public class NotificationsApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<NotificationsApplicationModule>();
        context.Services.AddTransient<IResilientHttpRequest, ResilientHttpRequest>();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<NotificationsApplicationModule>(validate: true);
        });

        context.Services.AddHttpClientProxies(
           typeof(NotificationsApplicationContractsModule).Assembly,
                  NotificationsRemoteServiceConsts.RemoteServiceName
        );

        context.Services.ConfigureRabbitMQ();
        context.Services.AddQueueMessageConsumer<EmailConsumer, EmailMessages>();

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        // Set the max defaults as max - we are using non serverside paging and this effect this
        ExtensibleLimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        ExtensibleLimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;

        LimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        LimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;
    }
}