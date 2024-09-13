using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Application;
using Volo.Abp.VirtualFileSystem;
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
using Unity.Shared.MessageBrokers.RabbitMQ;
using RabbitMQ.Client;
using Unity.Shared.MessageBrokers.RabbitMQ.Constants;
using Unity.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Payments.RabbitMQ.QueueMessages;
using Unity.Payments.Integrations.RabbitMQ;

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

        Configure<PaymentRequestBackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
            options.PaymentRequestOptions.ProducerExpression = configuration.GetValue<string>("BackgroundJobs:CasPaymentsReconciliation:ProducerExpression") ?? "";
            options.FinancialNotificationSummaryOptions.ProducerExpression = configuration.GetValue<string>("BackgroundJobs:CasFinancialNotificationSummary:ProducerExpression") ?? "";            
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
    }
}
