using Volo.Abp.Modularity;
using Volo.Abp.Localization;
using Unity.Reporting.Localization;
using Volo.Abp.Domain;
using Volo.Abp.Localization.ExceptionHandling;
using Volo.Abp.Validation;
using Volo.Abp.Validation.Localization;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.Settings;

namespace Unity.Reporting;

/// <summary>
/// ABP Framework module for Unity.Reporting Domain Shared layer providing common domain types, localization resources, and shared configuration.
/// Configures localization for the Unity.Reporting module, registers virtual file system resources for embedded localization files,
/// and sets up exception handling localization mapping. This module contains shared domain concepts used across multiple layers
/// of the reporting system including enums, constants, and localization resources.
/// </summary>
[DependsOn(
    typeof(AbpValidationModule),
    typeof(AbpDddDomainSharedModule),
    typeof(AbpSettingsModule)
)]
public class ReportingDomainSharedModule : AbpModule
{
    /// <summary>
    /// Configures services for the Unity.Reporting Domain Shared module including localization, virtual file system, and exception handling.
    /// Sets up embedded resources for localization files, configures the ReportingResource with English as the default language,
    /// and maps exception handling to use the reporting localization namespace for consistent error messaging.
    /// </summary>
    /// <param name="context">The service configuration context for dependency injection setup and module configuration.</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<ReportingDomainSharedModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<ReportingResource>("en")
                .AddBaseTypes(typeof(AbpValidationResource))
                .AddVirtualJson("/Localization/Reporting");
        });

        Configure<AbpExceptionLocalizationOptions>(options =>
        {
            options.MapCodeNamespace("Reporting", typeof(ReportingResource));
        });
    }
}
