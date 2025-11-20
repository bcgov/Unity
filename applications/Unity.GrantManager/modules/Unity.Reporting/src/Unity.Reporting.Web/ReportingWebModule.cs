using Microsoft.Extensions.DependencyInjection;
using Unity.Reporting.Localization;
using Unity.Reporting.Web.Menus;
using Volo.Abp.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.SettingManagement.Web;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;

namespace Unity.Reporting.Web;

/// <summary>
/// ABP Framework module for Unity.Reporting Web layer providing MVC controllers, Razor Pages, view components, and web assets.
/// Configures ASP.NET Core MVC integration, localization resources, navigation menus, virtual file system for embedded resources,
/// and AutoMapper profiles for web-specific data transformations. Handles the presentation layer concerns for the Unity
/// Reporting module including user interfaces for report configuration and administration.
/// </summary>
[DependsOn(
    typeof(ReportingApplicationContractsModule),
    typeof(AbpAspNetCoreMvcUiThemeSharedModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpSettingManagementWebModule)
    )]
public class ReportingWebModule : AbpModule
{
    /// <summary>
    /// Pre-configures services for the Unity.Reporting Web module before main service configuration.
    /// Sets up MVC data annotations localization to use Unity.Reporting localization resources
    /// and registers this assembly as an application part for ASP.NET Core MVC controller discovery.
    /// </summary>
    /// <param name="context">The service configuration context for dependency injection setup.</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.PreConfigure<AbpMvcDataAnnotationsLocalizationOptions>(options =>
        {
            options.AddAssemblyResource(typeof(ReportingResource), typeof(ReportingWebModule).Assembly);
        });

        PreConfigure<IMvcBuilder>(mvcBuilder =>
        {
            mvcBuilder.AddApplicationPartIfNotExists(typeof(ReportingWebModule).Assembly);
        });
    }

    /// <summary>
    /// Configures main services for the Unity.Reporting Web module including navigation, virtual file system, and AutoMapper.
    /// Registers the ReportingMenuContributor for navigation menu setup, configures embedded virtual file system resources
    /// for CSS/JS assets and views, and sets up AutoMapper object mapping with validation for web-layer data transformations.
    /// </summary>
    /// <param name="context">The service configuration context for dependency injection and module configuration.</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new ReportingMenuContributor());
        });

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<ReportingWebModule>();
        });

        context.Services.AddAutoMapperObjectMapper<ReportingWebModule>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<ReportingWebModule>(validate: true);
        });
    }
}
