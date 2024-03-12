using Microsoft.Extensions.DependencyInjection;
using Unity.AspNetCore.Mvc.UI.Theme.Standard.Bundling;
using Unity.AspNetCore.Mvc.UI.Themes.Standard.Bundling;
using Unity.AspNetCore.Mvc.UI.Themes.Standard.Toolbars;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.Toolbars;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace Unity.AspNetCore.Mvc.UI.Themes.Standard;

[DependsOn(
    typeof(AbpAspNetCoreMvcUiThemeSharedModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule)
    )]
public class UnityAspNetCoreMvcUiThemesModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(UnityAspNetCoreMvcUiThemesModule).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpThemingOptions>(options =>
        {
            options.Themes.Add<StandardTheme>();

            if (options.DefaultThemeName == null)
            {
                options.DefaultThemeName = StandardTheme.Name;
            }
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<UnityAspNetCoreMvcUiThemesModule>("Unity.AspNetCore.Mvc.UI.Themes");
        });

        Configure<AbpToolbarOptions>(options =>
        {
            options.Contributors.Add(new StandardThemeMainTopToolbarContributor());
        });

        Configure<AbpBundlingOptions>(options =>
        {
            options
                .StyleBundles
                .Add(StandardThemeBundles.Styles.Global, bundle =>
                {
                    bundle
                        .AddBaseBundles(StandardBundles.Styles.Global)
                        .AddContributors(typeof(StandardThemeGlobalStyleContributor));
                });

            options
                .ScriptBundles
                .Add(StandardThemeBundles.Scripts.Global, bundle =>
                {
                    bundle
                        .AddBaseBundles(StandardBundles.Scripts.Global)
                        .AddContributors(typeof(StandardThemeGlobalScriptContributor));
                });
        });
    }
}
