using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Unity.GrantManager.Web.Pages.GrantApplications.Components.AssessmentResults
{

    [Widget(
    ScriptFiles = new[] {
        "/Pages/GrantApplications/Components/AssessmentResults/Default.js"
    },
    StyleFiles = new[] {
        "/Pages/GrantApplications/Components/AssessmentResults/Default.css"
    })]
    public class AssessmentResults : AbpViewComponent
    {

        

        public IViewComponentResult Invoke()
        {
            AssessmentResultsViewModel model = new()
            {
                ProjectSummary = "",
                TotalScore = null,
                ApprovedAmount = null,
                Recommendation = null,
            };

            return View(model);
        }
    }
}
