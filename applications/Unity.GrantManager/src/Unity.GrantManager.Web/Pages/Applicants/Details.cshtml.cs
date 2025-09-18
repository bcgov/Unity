using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Unity.GrantManager.Permissions;

namespace Unity.GrantManager.Web.Pages.Applicants
{
    [Authorize(GrantApplicationPermissions.Applicants.ViewList)]
    public class DetailsModel : GrantManagerPageModel
    {

        [BindProperty(SupportsGet = true)]
        public Guid ApplicantId { get; set; }

        public DetailsModel()
        {

        }

        public IActionResult OnGet()
        {
            if (ApplicantId == Guid.Empty)
            {
                return NotFound();
            }

            return Page();
        }
    }
}