using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? FormId { get; set; }

        public void OnGet()
        {     
        }
    }
}
