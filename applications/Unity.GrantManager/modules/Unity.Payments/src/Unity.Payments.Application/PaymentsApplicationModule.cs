using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.AspNetCore.Mvc;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers.Quartz;

using Volo.Abp.TenantManagement;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ;
using Unity.Payments.RabbitMQ.QueueMessages;
using Unity.Payments.Integrations.RabbitMQ;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.ExceptionHandling;

namespace Unity.Payments;

[DependsOn(
    typeof(AbpVirtualFileSystemModule),
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpVirtualFileSystemModule),
    typeof(PaymentsApplicationContractsModule),
    typeof(AbpBackgroundJobsModule),
    typeof(AbpBackgroundWorkersQuartzModule),
    typeof(AbpTenantManagementDomainModule)
    )]
public class PaymentsApplicationModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(PaymentsApplicationModule).Assembly);
        });
    }


    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.ConfigureRabbitMQ();
        context.Services.AddQueueMessageConsumer<InvoiceConsumer, InvoiceMessages>();
        context.Services.AddQueueMessageConsumer<ReconciliationConsumer, ReconcilePaymentMessages>();

        Configure<AbpMultiTenancyOptions>(options =>
        {
            options.IsEnabled = true;
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<PaymentsApplicationModule>();
        });

        context.Services.AddAutoMapperObjectMapper<PaymentsApplicationModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<PaymentsApplicationModule>(validate: true);
        });

        context.Services.AddHttpClientProxies(
           typeof(PaymentsApplicationContractsModule).Assembly,
           PaymentsRemoteServiceConsts.RemoteServiceName
        );

        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(PaymentsApplicationModule).Assembly);
        });

        context.Services.AddAssemblyOf<PaymentsApplicationModule>();

        context.Services.AddAbpDbContext<PaymentsDbContext>(options =>
        {
            /* Add custom repositories here. */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpExceptionHandlingOptions>(options =>
        {
            options.SendExceptionsDetailsToClients = true;
            options.SendStackTraceToClients = false;
        });

        // Set the max defaults as max - we are using non serverside paging and this effect this
        ExtensibleLimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        ExtensibleLimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;

        LimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        LimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;
    }

    
}
