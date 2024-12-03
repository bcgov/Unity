using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Unity.Notifications.Localization;
using Unity.Notifications.Web.Menus;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;
using Unity.Notifications.Web.Settings;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.Notifications.Web.Settings.NotificationsSettingGroup;

namespace Unity.Notifications.Web;

[DependsOn(
    typeof(NotificationsApplicationContractsModule),
    typeof(AbpAspNetCoreMvcUiThemeSharedModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpSettingManagementWebModule)
    )]
public class NotificationsWebModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(typeof(NotificationsResource), typeof(NotificationsWebModule).Assembly);
        });

        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(NotificationsWebModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = configuration.GetValue<bool>("BackgroundJobs:IsJobExecutionEnabled");
        });

        Configure<AbpBackgroundWorkerQuartzOptions>(options =>
        {
            options.IsAutoRegisterEnabled = configuration.GetValue<bool>("BackgroundJobs:Quartz:IsAutoRegisterEnabled");
        });

        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new NotificationsMenuContributor());
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<NotificationsWebModule>();
        });

        Configure<AbpBundlingOptions>(options =>
        {
            options.ScriptBundles.Configure(
                typeof(Volo.Abp.SettingManagement.Web.Pages.SettingManagement.IndexModel).FullName,
                configuration =>
                {
                    configuration.AddContributors(typeof(NotificationsSettingScriptBundleContributor));
                });
        });

        Configure<SettingManagementPageOptions>(options =>
        {
            options.Contributors.Add(new NotificationsSettingPageContributor());
        });

        context.Services.AddAutoMapperObjectMapper<NotificationsWebModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<NotificationsWebModule>(validate: true);
        });

        Configure<RazorPagesOptions>(options =>
        {
            //Configure authorization.
        });
    }
}
