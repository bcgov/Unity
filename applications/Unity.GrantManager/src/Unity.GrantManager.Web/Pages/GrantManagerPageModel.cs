using Unity.GrantManager.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class GrantManagerPageModel : AbpPageModel
{
    protected GrantManagerPageModel()
    {
        LocalizationResourceType = typeof(GrantManagerResource);
    }
}
