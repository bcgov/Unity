using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared.Permissions;

namespace Unity.GrantManager.Web.Pages.Settings.ExceptionLogs
{
    [Authorize(IdentityConsts.ITOperationsPolicyName)]
    public class IndexModel : GrantManagerPageModel
    {
        public void OnGet()
        {
        }
    }
}
