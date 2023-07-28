using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    public class DetailsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string SubmissionId { get; set; }
        public void OnGet()
        {
        }
    }
}
