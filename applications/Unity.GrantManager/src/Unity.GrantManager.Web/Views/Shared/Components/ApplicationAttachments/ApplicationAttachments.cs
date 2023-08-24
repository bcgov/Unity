using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationAttachments
{

    [Widget(ScriptFiles = new[] { "/Views/Shared/Components/ApplicationAttachments/ApplicationAttachments.js", "/libs/pubsub-js/src/pubsub.js" },
        StyleFiles = new[] { "/Views/Shared/Components/ApplicationAttachments/ApplicationAttachments.css" })]
    public class ApplicationAttachments : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
