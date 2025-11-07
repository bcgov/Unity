using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;

namespace Unity.GrantManager.Web.Pages.Applicants
{
    [Authorize(GrantApplicationPermissions.Applicants.ViewList)]
    public class DetailsModel : GrantManagerPageModel
    {
        private readonly IApplicantRepository _applicantRepository;

        [BindProperty(SupportsGet = true)]
        public Guid ApplicantId { get; set; }

        public Applicant? Applicant { get; set; }
        public string ApplicantDisplayName { get; set; } = string.Empty;
        public string UnityApplicantId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public DetailsModel(IApplicantRepository applicantRepository)
        {
            _applicantRepository = applicantRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (ApplicantId == Guid.Empty)
            {
                return NotFound();
            }

            try
            {
                Applicant = await _applicantRepository.GetAsync(ApplicantId);

                // Set properties for breadcrumb and display
                ApplicantDisplayName = !string.IsNullOrEmpty(Applicant.ApplicantName)
                    ? Applicant.ApplicantName
                    : "Unknown Applicant";

                UnityApplicantId = Applicant.UnityApplicantId ?? "N/A";
                Status = Applicant.Status ?? "Active";

                return Page();
            }
            catch (Exception)
            {
                return NotFound();
            }
        }
    }
}