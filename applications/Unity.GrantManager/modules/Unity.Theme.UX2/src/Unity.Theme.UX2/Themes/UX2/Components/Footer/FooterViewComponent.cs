using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Themes.UX2.Components.Footer;

public class FooterViewComponent : AbpViewComponent
{
    public virtual IViewComponentResult Invoke()
    {
        return View("~/Themes/UX2/Components/Footer/Default.cshtml");
    }
}
