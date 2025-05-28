using Unity.Reporting.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Reporting.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class ReportingPageModel : AbpPageModel
{
    protected ReportingPageModel()
    {
        LocalizationResourceType = typeof(ReportingResource);
        ObjectMapperContext = typeof(ReportingWebModule);
    }
}
