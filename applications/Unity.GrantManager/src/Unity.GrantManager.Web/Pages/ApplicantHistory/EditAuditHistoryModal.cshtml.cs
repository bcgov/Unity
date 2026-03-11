using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class EditAuditHistoryModal : AbpPageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public AuditHistoryModalViewModel? AuditHistoryForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public EditAuditHistoryModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public async Task OnGetAsync(Guid id)
    {
        Id = id;
        var record = await _applicantHistoryAppService.GetAuditHistoryAsync(id);
        AuditHistoryForm = new AuditHistoryModalViewModel
        {
            ApplicantId = record.ApplicantId ?? Guid.Empty,
            AuditTrackingNumber = record.AuditTrackingNumber,
            AuditDate = record.AuditDate,
            AuditNote = record.AuditNote
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var dto = new CreateUpdateAuditHistoryDto
        {
            ApplicantId = AuditHistoryForm!.ApplicantId,
            AuditTrackingNumber = AuditHistoryForm.AuditTrackingNumber,
            AuditDate = AuditHistoryForm.AuditDate,
            AuditNote = AuditHistoryForm.AuditNote
        };

        await _applicantHistoryAppService.UpdateAuditHistoryAsync(Id, dto);
        return NoContent();
    }
}
