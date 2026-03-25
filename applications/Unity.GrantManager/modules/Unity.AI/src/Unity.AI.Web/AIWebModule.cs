using Microsoft.Extensions.DependencyInjection;
using Unity.AI.Localization;
using Unity.AI.Web.Views.Settings;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.SettingManagement.Web.Pages.SettingManagement;
using Volo.Abp.VirtualFileSystem;

namespace Unity.AI.Web;

[DependsOn(
    typeof(AIApplicationModule),
    typeof(AbpAspNetCoreMvcUiThemeSharedModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpSettingManagementWebModule)
    )]
public class AIWebModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(typeof(AIResource), typeof(AIWebModule).Assembly);
        });

        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(AIWebModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AIWebModule>();
        });

        context.Services.AddAutoMapperObjectMapper<AIWebModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AIWebModule>(validate: true);
        });

        Configure<SettingManagementPageOptions>(options =>
        {
            options.Contributors.Add(new AISettingPageContributor());
        });
    }
}
