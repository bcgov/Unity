﻿using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.AspNetCore.Mvc.UI.Themes.Themes.Standard.Components.MainNavbar;

public class TopbarViewComponent : AbpViewComponent
{
    public virtual IViewComponentResult Invoke()
    {
        return View("~/Themes/Standard/Components/Topbar/Default.cshtml");
    }
}