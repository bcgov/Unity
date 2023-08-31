using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ReviewList
{

    [Widget(RefreshUrl = "Widgets/Comments",ScriptFiles = new[] { "/Views/Shared/Components/Comments/Comments.js", "/libs/pubsub-js/src/pubsub.js" },
        StyleFiles = new[] { "/Views/Shared/Components/Comments/Comments.css" })]
    public class Comments : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
