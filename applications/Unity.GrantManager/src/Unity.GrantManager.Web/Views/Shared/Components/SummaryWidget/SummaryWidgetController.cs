using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Widgets/Summary")]
    [Route("GrantApplications/Widgets/Summary")]
    public class SummaryWidgetController : AbpController
    {
        
        [HttpGet]
        [Route("RefreshSummary")]
        public IActionResult Summary(Guid applicationId, Boolean isReadOnly = false)
        {
            return ViewComponent("SummaryWidget", new { applicationId, isReadOnly });
        }
        
    }
}
