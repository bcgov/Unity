using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Unity.Payments.PaymentRequests;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.Quartz;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using Volo.Abp.Threading;

namespace Unity.Payments;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpBackgroundWorkersQuartzModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(AbpTenantManagementDomainModule)
    )]
public class PaymentsTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddAlwaysAllowAuthorization();
        Configure<PaymentRequestBackgroundJobsOptions>(options =>
        {
            options.IsJobExecutionEnabled = false;
            options.PaymentRequestOptions.ProducerExpression = "0 0 12 * * ? *";
        });
        Configure<AbpBackgroundWorkerQuartzOptions>(options => { options.IsAutoRegisterEnabled = false; });
    }

    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        Quartz.Logging.LogContext.SetCurrentLogProvider(NullLoggerFactory.Instance);
        PreConfigure<AbpQuartzOptions>(options =>
        {
            options.Configurator = configure =>
            {
                configure.SchedulerName = Guid.NewGuid().ToString();
            };
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        SeedTestData(context);
    }

    private static void SeedTestData(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(async () =>
        {
            using var scope = context.ServiceProvider.CreateScope();
            await scope.ServiceProvider
                .GetRequiredService<IDataSeeder>()
                .SeedAsync();
        });
    }
}
