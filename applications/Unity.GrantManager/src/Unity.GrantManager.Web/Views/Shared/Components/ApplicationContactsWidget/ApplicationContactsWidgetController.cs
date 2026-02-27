using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationContactsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Widgets/ApplicationContacts")]
    [Route("GrantApplications/Widgets/ApplicationContacts")]
    public class ApplicationContactsWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshApplicationContacts")]
        public IActionResult ApplicationContacts(Guid applicationId)
        { 
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for ApplicationContactsWidgetController: RefreshApplicationContacts");
                return ViewComponent("ApplicationContactsWidget", new { applicationId });
            }
            return ViewComponent("ApplicationContactsWidget", new { applicationId });
        }
    }
}
