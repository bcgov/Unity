using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;
using Volo.Abp.TenantManagement;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Pages.Applicants
{
    [Authorize(UnitySelector.ApplicantManagement.Applicant.Default)]
    public class DetailsModel : GrantManagerPageModel
    {
        private readonly IApplicantRepository _applicantRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly ITenantRepository _tenantRepository;

        [BindProperty(SupportsGet = true)]
        public Guid ApplicantId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ApplicationId { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public Guid? TenantId { get; set; }

        public Applicant? Applicant { get; set; }
        public bool ApplicantIsDeleted { get; set; }
        public string ApplicantDisplayName { get; set; } = string.Empty;
        public string UnityApplicantId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? CurrentUserId { get; set; }
        public string CurrentUserName { get; set; }
        public string AllowedFileTypes { get; set; } = string.Empty;
        public string MaxFileSize { get; set; } = string.Empty;

        public DetailsModel(
            IApplicantRepository applicantRepository,
            IApplicationRepository applicationRepository,
            ITenantRepository tenantRepository,
            ICurrentUser currentUser,
            IConfiguration configuration)
        {
            _applicantRepository = applicantRepository;
            _applicationRepository = applicationRepository;
            _tenantRepository = tenantRepository;
            CurrentUserId = currentUser.Id;
            CurrentUserName = currentUser.SurName + ", " + currentUser.Name;
            AllowedFileTypes = configuration["S3:AllowedFileTypes"] ?? "";
            MaxFileSize = configuration["S3:MaxFileSize"] ?? "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (TenantId.HasValue && TenantId.Value != CurrentTenant.Id)
            {
                var applicationTenant = await _tenantRepository.FindAsync(TenantId.Value);
                return RedirectToPage("/Error", new
                {
                    httpStatusCode = 409,
                    entityType = "Applicant",
                    applicationTenantName = applicationTenant?.Name ?? TenantId.Value.ToString(),
                    currentTenantName = CurrentTenant.Name ?? "Host"
                });
            }

            // Resolve ApplicantId from ApplicationId if needed
            if (ApplicantId == Guid.Empty && ApplicationId.HasValue)
            {
                try
                {
                    var application = await _applicationRepository.WithBasicDetailsAsync(ApplicationId.Value);
                    ApplicantId = application.ApplicantId;
                }
                catch (Exception)
                {
                    return RedirectToPage("/Error", new
                    {
                        httpStatusCode = 404,
                        entityType = "Applicant",
                        currentTenantName = CurrentTenant.Name ?? "Host"
                    });
                }
            }

            if (ApplicantId == Guid.Empty)
            {
                return NotFound();
            }

            try
            {
                Applicant = await _applicantRepository.GetAsync(ApplicantId);

                ApplicantDisplayName = !string.IsNullOrEmpty(Applicant.ApplicantName)
                    ? Applicant.ApplicantName
                    : "Applicant Name";

                if (Applicant.IsDeleted)
                {
                    ApplicantIsDeleted = true;
                    return Page();
                }

                UnityApplicantId = Applicant.UnityApplicantId ?? "N/A";
                Status = Applicant.Status ?? string.Empty;

                return Page();
            }
            catch (Exception)
            {
                return RedirectToPage("/Error", new
                {
                    httpStatusCode = 404,
                    entityType = "Applicant",
                    currentTenantName = CurrentTenant.Name ?? "Host"
                });
            }
        }
    }
}