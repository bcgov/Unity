using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.UserInfoWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/UserInfo")]
    public class UserInfoWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshUserInfo")]
        public IActionResult UserInfo(string name, string info)
        {
            return ViewComponent("UserInfoWidget", new { name, info });
        }
    }
}
