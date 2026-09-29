using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Unity.Notifications.Permissions;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailsWidget
{
    [Authorize(NotificationsPermissions.Email.Default)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Emails")]
    public class EmailsWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("/GrantApplicants/Widgets/Emails/RefreshEmails")]
        [Route("RefreshEmails")]
        public IActionResult Emails(Guid applicationId, Guid currentUserId, Guid applicantId = default)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for EmailsWidgetController: RefreshEmails");
                return ViewComponent("EmailsWidget");
            }
            return ViewComponent("EmailsWidget", new { applicationId, currentUserId, applicantId });
        }
    }
}
