using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class EditReportsHistoryModal : AbpPageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public ReportsHistoryModalViewModel? ReportsHistoryForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public EditReportsHistoryModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public async Task OnGetAsync(Guid id)
    {
        Id = id;
        var record = await _applicantHistoryAppService.GetReportsHistoryAsync(id);
        ReportsHistoryForm = new ReportsHistoryModalViewModel
        {
            ApplicantId = record.ApplicantId ?? Guid.Empty,
            FiscalYear = record.FiscalYear,
            ReportDate = record.ReportDate,
            Outstanding = record.Outstanding,
            IncompleteReport = record.IncompleteReport,
            Note = record.Note
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
            IncompleteReport = ReportsHistoryForm.IncompleteReport,
            Note = ReportsHistoryForm.Note
        };

        await _applicantHistoryAppService.UpdateReportsHistoryAsync(Id, dto);
        return NoContent();
    }
}
