using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ActionBar
{
    [Widget(ScriptFiles = new[] { "/Views/Shared/Components/ActionBar/Default.js" },
        StyleFiles = new[] { "/Views/Shared/Components/ActionBar/Default.css" })]
      public class ActionBar : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}

