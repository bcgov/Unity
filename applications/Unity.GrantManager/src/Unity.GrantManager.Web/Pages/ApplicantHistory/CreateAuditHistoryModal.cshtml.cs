using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class CreateAuditHistoryModal : AbpPageModel
{
    [BindProperty]
    public AuditHistoryModalViewModel? AuditHistoryForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public CreateAuditHistoryModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public void OnGet(Guid applicantId)
    {
        AuditHistoryForm = new AuditHistoryModalViewModel
        {
            ApplicantId = applicantId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var dto = new CreateUpdateAuditHistoryDto
        {
            ApplicantId = AuditHistoryForm!.ApplicantId,
            AuditTrackingNumber = AuditHistoryForm.AuditTrackingNumber,
            AuditDate = AuditHistoryForm.AuditDate,
            AuditStatus = Enum.TryParse<AuditHistoryStatus>(AuditHistoryForm.AuditStatus, out var status) ? status : null,
            AuditorName = AuditHistoryForm.AuditorName,
            AuditNote = AuditHistoryForm.AuditNote
        };

        await _applicantHistoryAppService.CreateAuditHistoryAsync(dto);
        return NoContent();
    }
}
