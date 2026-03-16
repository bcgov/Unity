using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class EditIssueTrackingModal : AbpPageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public IssueTrackingModalViewModel? IssueTrackingForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public EditIssueTrackingModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public async Task OnGetAsync(Guid id)
    {
        Id = id;
        var record = await _applicantHistoryAppService.GetIssueTrackingAsync(id);
        IssueTrackingForm = new IssueTrackingModalViewModel
        {
            ApplicantId = record.ApplicantId ?? Guid.Empty,
            Year = record.Year,
            IssueHeading = record.IssueHeading,
            IssueDescription = record.IssueDescription,
            Resolved = record.Resolved,
            ResolutionNote = record.ResolutionNote
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

        await _applicantHistoryAppService.UpdateIssueTrackingAsync(Id, dto);
        return NoContent();
    }
}
