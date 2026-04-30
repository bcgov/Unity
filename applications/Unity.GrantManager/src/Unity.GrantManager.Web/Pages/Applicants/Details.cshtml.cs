using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Pages.Applicants;

[Authorize(UnitySelector.ApplicantManagement.Default)]
public class DetailsModel : GrantManagerPageModel
{
    private readonly IApplicantRepository _applicantRepository;
    private readonly IApplicationRepository _applicationRepository;

    [BindProperty(SupportsGet = true)]
    public Guid ApplicantId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ApplicationId { get; set; } = null;

    public Applicant? Applicant { get; set; }
    public string ApplicantDisplayName { get; set; } = string.Empty;
    public string UnityApplicantId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CurrentUserId { get; set; }
    public string CurrentUserName { get; set; }
    public string Extensions { get; set; } = string.Empty;
    public string MaxFileSize { get; set; } = string.Empty;

    public DetailsModel(
        IApplicantRepository applicantRepository,
        IApplicationRepository applicationRepository,
        ICurrentUser currentUser,
        IConfiguration configuration)
    {
        _applicantRepository = applicantRepository;
        _applicationRepository = applicationRepository;
        CurrentUserId = currentUser.Id;
        CurrentUserName = currentUser.SurName + ", " + currentUser.Name;
        Extensions = configuration["S3:DisallowedFileTypes"] ?? "";
        MaxFileSize = configuration["S3:MaxFileSize"] ?? "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
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
                return NotFound();
            }
        }

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
                : "Applicant Name";

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