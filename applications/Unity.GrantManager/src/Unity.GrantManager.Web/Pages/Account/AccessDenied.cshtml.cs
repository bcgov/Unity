using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.Account.Web.Pages.Account;

namespace Unity.GrantManager.Web.Pages.Account
{
    public class AccessDeniedModel : AccountPageModel
    {
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string ReturnUrlHash { get; set; } = string.Empty;

        public virtual Task<IActionResult> OnGetAsync()
        {
            return Task.FromResult<IActionResult>(Page());
        }

        public virtual Task<IActionResult> OnPostAsync()
        {
            return Task.FromResult<IActionResult>(Page());
        }
    }
}