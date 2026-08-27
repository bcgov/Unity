using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class CreateReportsHistoryModal : AbpPageModel
{
    [BindProperty]
    public ReportsHistoryModalViewModel? ReportsHistoryForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public CreateReportsHistoryModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public void OnGet(Guid applicantId)
    {
        ReportsHistoryForm = new ReportsHistoryModalViewModel
        {
            ApplicantId = applicantId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var dto = new CreateUpdateReportsHistoryDto
        {
            ApplicantId = ReportsHistoryForm!.ApplicantId,
            FiscalYear = ReportsHistoryForm.FiscalYear,
            ReportDate = ReportsHistoryForm.ReportDate,
            Outstanding = ReportsHistoryForm.Outstanding,
            SignedOff = ReportsHistoryForm.SignedOff,
            IncompleteReport = ReportsHistoryForm.IncompleteReport,
            Note = ReportsHistoryForm.Note
        };

        await _applicantHistoryAppService.CreateReportsHistoryAsync(dto);
        return NoContent();
    }
}
