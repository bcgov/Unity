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
using Unity.Payments.PaymentRequests;
using Volo.Abp.TenantManagement;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ;
using Unity.Payments.RabbitMQ.QueueMessages;
using Unity.Payments.Integrations.RabbitMQ;
using Volo.Abp.Application.Dtos;
using Volo.Abp.SettingManagement;
using Unity.GrantManager.Settings;
using Volo.Abp;

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

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        ISettingManager? settingManager = context.ServiceProvider.GetService<ISettingManager>();
        if (settingManager != null)
        {
            ConfigureBackgroundServices(settingManager);
        }
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

        // Set the max defaults as max - we are using non serverside paging and this effect this
        ExtensibleLimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        ExtensibleLimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;

        LimitedResultRequestDto.DefaultMaxResultCount = int.MaxValue;
        LimitedResultRequestDto.MaxMaxResultCount = int.MaxValue;
    }

    private void ConfigureBackgroundServices(ISettingManager settingManager)
    {
        string isJobExecutionEnabled = GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.IsJobExecutionEnabled);
        bool isJobExecutionEnabledBool = isJobExecutionEnabled == "True";
        if (isJobExecutionEnabledBool) return;

        string casPaymentsProducerExpression = GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.CasPaymentsReconciliation_ProducerExpression);
        string casFinancialNotificationExpression = GetSettingsValue(settingManager, SettingsConstants.BackgroundJobs.CasFinancialNotificationSummary_ProducerExpression);

        // Configure the payment request background job options
        Configure<PaymentRequestBackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = isJobExecutionEnabledBool;
            options.PaymentRequestOptions.ProducerExpression = casPaymentsProducerExpression;
            options.FinancialNotificationSummaryOptions.ProducerExpression = casFinancialNotificationExpression;
        });
    }

    private static string GetSettingsValue(ISettingManager settingManager, string settingName)
    {
        // Fetch the producer expression synchronously
        var settingValue = settingManager.GetOrNullDefaultAsync(settingName, fallback: true).Result;
        return !string.IsNullOrEmpty(settingValue) ? settingValue : string.Empty;
    }
}
