using Unity.Flex.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Flex.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class FlexPageModel : AbpPageModel
{
    protected FlexPageModel()
    {
        LocalizationResourceType = typeof(FlexResource);
        ObjectMapperContext = typeof(FlexWebModule);
    }
}
