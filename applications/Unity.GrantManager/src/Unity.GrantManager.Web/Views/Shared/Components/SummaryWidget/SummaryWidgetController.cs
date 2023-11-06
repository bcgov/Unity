using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Widgets/Summary")]
    public class SummaryWidgetController : AbpController
    {
        
        [HttpGet]
        [Route("RefreshSummary")]
        public IActionResult Summary(Guid applicationId)
        {
            return ViewComponent("SummaryWidget", new { applicationId });
        }
        
    }
}
