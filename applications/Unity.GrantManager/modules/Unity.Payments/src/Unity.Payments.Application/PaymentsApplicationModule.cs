using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Localization.ExceptionHandling;
using Unity.Payments.Localization;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Localization;
using Volo.Abp.Validation.Localization;
using Localization.Resources.AbpUi;
using Volo.Abp.AspNetCore.Mvc;
using Unity.Payments.EntityFrameworkCore;
using Volo.Abp.MultiTenancy;
using Volo.Abp.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Volo.Abp.BackgroundWorkers.Quartz;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Quartz;
using System;
using Volo.Abp.TenantManagement;


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

        PreConfigure<AbpQuartzOptions>(options =>
        {
            options.Configurator = configure =>
            {
                configure.SchedulerName = Guid.NewGuid().ToString();
            };
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<CasPaymentRequestBackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
            options.PaymentRequestOptions.ProducerExpression = configuration.GetValue<string>("BackgroundJobs:CasPaymentsReconciliation:ProducerExpression") ?? "";
            options.PaymentRequestOptions.ConsumerExpression = configuration.GetValue<string>("BackgroundJobs:CasPaymentsReconciliation:ConsumerExpression") ?? "";
        });

        Configure<RabbitMQOptions>(options =>
        {
            options.HostName = configuration.GetValue<string>("RabbitMQ:HostName") ?? "";
            options.Port = configuration.GetValue<int>("RabbitMQ:Port");
            options.UserName = configuration.GetValue<string>("RabbitMQ:UserName") ?? "";
            options.Password = configuration.GetValue<string>("RabbitMQ:Password") ?? "";
            options.VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost") ?? "";
        });

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
    }
}
