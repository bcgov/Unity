using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Identity.Web.Pages.Identity;

public abstract class IdentityPageModel : AbpPageModel
{
    protected IdentityPageModel()
    {
        ObjectMapperContext = typeof(UnitydentityWebModule);
    }
}
