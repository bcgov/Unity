using Unity.GrantManager.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class GrantManagerController : AbpControllerBase
{
    protected GrantManagerController()
    {
        LocalizationResource = typeof(GrantManagerResource);
    }
}
