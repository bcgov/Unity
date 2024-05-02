using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationBreadcrumbWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/ApplicationBreadcrumb")]
    public class ApplicationBreadcrumbWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshApplicationBreadcrumb")]
        public IActionResult Status(Guid applicationId)
        {
            return ViewComponent("ApplicationBreadcrumbWidget", new { applicationId });
        }
    }
}
