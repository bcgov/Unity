using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Emails")]
    public class EmailsWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshEmails")]
        public IActionResult Emails(Guid ownerId, Guid currentUserId)
        { 
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for EmailsWidgetController: RefreshEmails");
                return ViewComponent("EmailsWidget");
            }
            return ViewComponent("EmailsWidget", new { ownerId, currentUserId });
        }
    }
}
