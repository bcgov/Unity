using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string SubmissionId { get; set; }
        public string selectedAction { get; set; }
        public IFormFile Attachment { get; set; } = null;
        public List<SelectListItem> ActionList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Y", Text = "Recommended for Approval"},
            new SelectListItem { Value = "N", Text = "Recommended for Denial"}
           
        };

        [TempData]
        public string SelectedApplicationId { get; set; } = "";
        public async Task OnGetAsync(string applicationId)
        {
            SelectedApplicationId = applicationId;
        }
    }
}
