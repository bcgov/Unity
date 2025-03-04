using Unity.Reporting.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Reporting;

public abstract class ReportingController : AbpControllerBase
{
    protected ReportingController()
    {
        LocalizationResource = typeof(ReportingResource);
    }
}
