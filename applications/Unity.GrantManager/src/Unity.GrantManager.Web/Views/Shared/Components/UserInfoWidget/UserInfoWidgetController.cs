using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.UserInfoWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/UserInfo")]
    public class UserInfoWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshUserInfo")]
        public IActionResult UserInfo(string displayName, string badge, string title)
        {
            return ViewComponent("UserInfoWidget", new { displayName, badge, title });
        }
    }
}
