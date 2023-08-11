using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize(Roles = "unity-manager")]
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? FormId { get; set; }

        public void OnGet()
        {
        }
    }
}
