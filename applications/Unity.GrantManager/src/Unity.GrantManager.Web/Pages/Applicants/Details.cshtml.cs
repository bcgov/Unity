using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Applicants
{
    [Authorize(GrantApplicationPermissions.Applicants.ViewList)]
    public class DetailsModel : GrantManagerPageModel
    {
        private readonly IApplicantAppService _applicantAppService;

        [BindProperty(SupportsGet = true)]
        public Guid ApplicantId { get; set; }

        public DetailsModel(IApplicantAppService applicantAppService)
        {
            _applicantAppService = applicantAppService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (ApplicantId == Guid.Empty)
            {
                return NotFound();
            }

            // TODO: When implementing full details functionality:
            // 1. Create ApplicantDto or ApplicantDetailsDto in Application.Contracts
            // 2. Add GetAsync(Guid id) method to IApplicantAppService
            // 3. Implement the method in ApplicantAppService
            // 4. Retrieve and display applicant details here

            return Page();
        }
    }
}