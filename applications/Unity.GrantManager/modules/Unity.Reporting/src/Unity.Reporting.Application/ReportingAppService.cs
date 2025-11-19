using Unity.Reporting.Localization;
using Volo.Abp.Application.Services;

namespace Unity.Reporting;

/// <summary>
/// Base application service class for Unity.Reporting module providing common configuration and localization setup.
/// Pre-configures localization resources and AutoMapper context for consistent behavior across all reporting services.
/// All reporting application services should inherit from this base class.
/// </summary>
public abstract class ReportingAppService : ApplicationService
{
    protected ReportingAppService()
    {
        LocalizationResource = typeof(ReportingResource);
        ObjectMapperContext = typeof(ReportingApplicationModule);
    }
}
