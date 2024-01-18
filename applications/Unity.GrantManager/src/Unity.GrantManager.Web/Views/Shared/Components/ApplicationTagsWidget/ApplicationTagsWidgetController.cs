using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationTagsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Tags")]
    public class ApplicationTagsWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshTags")]
        public IActionResult Status(Guid applicationId)
        {
            return ViewComponent("ApplicationTagsWidget", new { applicationId });
        }
    }
}
