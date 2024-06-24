using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Unity.Notifications.Integrations.Ches;
using Volo.Abp.BackgroundJobs;
using Unity.Notifications.EmailNotifications;
using Microsoft.Extensions.Configuration;
using Volo.Abp.BackgroundWorkers.Quartz;

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
            options.EmailResend.Expression = configuration.GetValue<string>("BackgroundJobs:EmailResend:Expression") ?? "";
            options.EmailResend.RetryAttemptsMaximum = configuration.GetValue<int>("BackgroundJobs:EmailResend:RetryAttemptsMaximum");
        });

        Configure<RabbitMQOptions>(options =>
        {
            options.HostName = configuration.GetValue<string>("RabbitMQ:HostName") ?? "";
            options.Port = configuration.GetValue<int>("RabbitMQ:Port");
            options.UserName = configuration.GetValue<string>("RabbitMQ:UserName") ?? "";
            options.Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "";
            options.VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "";
        });
    }
}