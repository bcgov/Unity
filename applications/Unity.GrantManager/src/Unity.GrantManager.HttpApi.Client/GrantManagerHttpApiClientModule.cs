using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.VirtualFileSystem;
using Unity.TenantManagement;
using Unity.Notifications;
using Unity.GrantManager.Integrations.Geocoder;
using System;

namespace Unity.GrantManager;

[DependsOn(
    typeof(GrantManagerApplicationContractsModule),
    typeof(AbpIdentityHttpApiClientModule),
    typeof(AbpPermissionManagementHttpApiClientModule),
    typeof(UnityTenantManagementHttpApiClientModule),
    typeof(AbpFeatureManagementHttpApiClientModule),
    typeof(AbpSettingManagementHttpApiClientModule),
    typeof(NotificationsHttpApiClientModule)
)]
public class GrantManagerHttpApiClientModule : AbpModule
{
    public const string RemoteServiceName = "Default";

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClientProxies(
            typeof(GrantManagerApplicationContractsModule).Assembly,
            RemoteServiceName
        );

        // Register Geocoder API client
        context.Services.AddHttpClient<IGeocoderApiService>(client =>
        {
            string baseAddress = "https://geocoder.api.gov.bc.ca";
            client.BaseAddress = new Uri(baseAddress);
        });


        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<GrantManagerHttpApiClientModule>();
        });
    }
}
