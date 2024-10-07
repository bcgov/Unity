using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationTagsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Tags")]
    public class ApplicationTagsWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshTags")]
        public IActionResult Status(Guid applicationId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for ApplicationTagsWidgetController: RefreshTags");
                return ViewComponent("ApplicationTagsWidget");
            }
            return ViewComponent("ApplicationTagsWidget", new { applicationId });
        }
    }
}
