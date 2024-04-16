using Microsoft.Extensions.DependencyInjection;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Toolbars;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.Toolbars;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;


namespace Unity.AspNetCore.Mvc.UI.Theme.UX2;

[DependsOn(
    typeof(AbpAspNetCoreMvcUiThemeSharedModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule)
    )]
public class UnityAspNetCoreMvcUIThemeUX2Module : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(UnityAspNetCoreMvcUIThemeUX2Module).Assembly);
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpThemingOptions>(options =>
        {
            options.Themes.Add<UnityUX2Theme>();

            if (options.DefaultThemeName == null)
            {
                options.DefaultThemeName = UnityUX2Theme.Name;
            }
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<UnityAspNetCoreMvcUIThemeUX2Module>("Unity.AspNetCore.Mvc.UI.Theme.UX2");
        });

        Configure<AbpToolbarOptions>(options =>
        {
            options.Contributors.Add(new UnityThemeMainTopToolbarContributor());
        });

        Configure<AbpBundlingOptions>(options =>
        {
            options
                .StyleBundles
                .Add(UnityThemeUX2Bundles.Styles.Global, bundle =>
                {
                    bundle
                        .AddBaseBundles(StandardBundles.Styles.Global)
                        .AddContributors(typeof(UnityThemeUX2GlobalStyleContributor));
                });

            options
                .ScriptBundles
                .Add(UnityThemeUX2Bundles.Scripts.Global, bundle =>
                {
                    bundle
                        .AddBaseBundles(StandardBundles.Scripts.Global)
                        .AddContributors(typeof(UnityThemeUX2GlobalScriptContributor));
                });
        });
    }
}
