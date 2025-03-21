﻿using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.UserInfoWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/UserInfo")]
    public class UserInfoWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshUserInfo")]
        public IActionResult UserInfo(string displayName, string badge, string title)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for UserInfoWidgetController: RefreshUserInfo");
                return ViewComponent("UserInfoWidget");
            }
            return ViewComponent("UserInfoWidget", new { displayName, badge, title });
        }
    }
}
