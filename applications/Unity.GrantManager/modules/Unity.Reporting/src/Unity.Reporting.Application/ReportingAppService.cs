using Unity.Reporting.Localization;
using Volo.Abp.Application.Services;

namespace Unity.Reporting;

public abstract class ReportingAppService : ApplicationService
{
    protected ReportingAppService()
    {
        LocalizationResource = typeof(ReportingResource);
        ObjectMapperContext = typeof(ReportingApplicationModule);
    }
}
