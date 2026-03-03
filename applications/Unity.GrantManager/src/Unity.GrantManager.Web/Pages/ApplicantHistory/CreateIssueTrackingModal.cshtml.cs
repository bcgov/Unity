using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class CreateIssueTrackingModal : AbpPageModel
{
    [BindProperty]
    public IssueTrackingModalViewModel? IssueTrackingForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public CreateIssueTrackingModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public void OnGet(Guid applicantId)
    {
        IssueTrackingForm = new IssueTrackingModalViewModel
        {
            ApplicantId = applicantId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var dto = new CreateUpdateIssueTrackingDto
        {
            ApplicantId = IssueTrackingForm!.ApplicantId,
            Year = IssueTrackingForm.Year,
            IssueHeading = IssueTrackingForm.IssueHeading,
            IssueDescription = IssueTrackingForm.IssueDescription,
            Resolved = IssueTrackingForm.Resolved,
            ResolutionNote = IssueTrackingForm.ResolutionNote
        };

        await _applicantHistoryAppService.CreateIssueTrackingAsync(dto);
        return NoContent();
    }
}
