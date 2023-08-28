using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResultAttachments
{

    [Widget(ScriptFiles = new[] { "/Views/Shared/Components/AssessmentResultAttachments/AssessmentResultAttachments.js", "/libs/pubsub-js/src/pubsub.js" },
        StyleFiles = new[] { "/Views/Shared/Components/AssessmentResultAttachments/AssessmentResultAttachments.css" })]
    public class AssessmentResultAttachments : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
