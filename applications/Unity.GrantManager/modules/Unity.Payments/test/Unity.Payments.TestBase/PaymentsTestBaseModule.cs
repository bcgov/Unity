using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.SettingManagement;
using Microsoft.Extensions.Logging.Abstractions;
using System;
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
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.Identity;


namespace Unity.Payments;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AbpBackgroundWorkersQuartzModule),
    typeof(AbpTenantManagementEntityFrameworkCoreModule),
    typeof(AbpTenantManagementDomainModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAuthorizationModule)
    )]
public class PaymentsTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAlwaysAllowAuthorization();

        // context.Services.AddAbpDbContext<PaymentsDbContext>(options =>
        // {
        //     /* Add custom repositories here. */
        //     options.AddDefaultRepositories(includeAllEntities: true);
        // });
 


        // context.Services.AddTransient<ISettingDefinitionRecordRepository, SettingDefinitionRecordRepository>();

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
