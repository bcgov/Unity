using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationStatusWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Status")]
    public class ApplicationStatusWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshStatus")]
        public IActionResult Status(Guid applicationId)
        {
            return ViewComponent("ApplicationStatusWidget", new { applicationId });
        }
    }
}
