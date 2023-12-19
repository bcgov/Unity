using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Threading;

namespace Unity.GrantManager.EntityFrameworkCore;

public static class GrantManagerEfCoreEntityExtensionMappings
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        GrantManagerGlobalFeatureConfigurator.Configure();
        GrantManagerModuleExtensionConfigurator.Configure();

        OneTimeRunner.Run(() =>
        {
            ObjectExtensionManager.Instance
                .MapEfCoreProperty<IdentityUser, string>(
                    "OidcSub",
                    (entityBuilder, propertyBuilder) => 
                    { 
                        propertyBuilder.HasDefaultValue(null); 
                    }
                );

            ObjectExtensionManager.Instance
                .MapEfCoreProperty<IdentityUser, string>(
                    "DisplayName",
                    (entityBuilder, propertyBuilder) => 
                    { 
                        propertyBuilder.HasDefaultValue(null); 
                    }
                );

            AbpIdentityDbProperties.DbTablePrefix = GrantManagerConsts.DbTablePrefix; //Identity            
            AbpAuditLoggingDbProperties.DbTablePrefix = GrantManagerConsts.DbTablePrefix; //Audit Logging            
            AbpPermissionManagementDbProperties.DbTablePrefix = GrantManagerConsts.DbTablePrefix; //Permission Management
            AbpCommonDbProperties.DbTablePrefix = GrantManagerConsts.DbTablePrefix; //Common
            AbpBackgroundJobsDbProperties.DbTablePrefix = GrantManagerConsts.DbTablePrefix; //Background Jobs
            AbpFeatureManagementDbProperties.DbTablePrefix = GrantManagerConsts.DbTablePrefix; //Feature Management
            AbpTenantManagementDbProperties.DbTablePrefix = GrantManagerConsts.DbTablePrefix; //Tenant Management 
        });
    }
}
