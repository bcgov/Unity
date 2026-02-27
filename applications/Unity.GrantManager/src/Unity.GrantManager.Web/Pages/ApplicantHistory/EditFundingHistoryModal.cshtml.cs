using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class EditFundingHistoryModal : AbpPageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public FundingHistoryModalViewModel? FundingHistoryForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public EditFundingHistoryModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public async Task OnGetAsync(Guid id)
    {
        Id = id;
        var record = await _applicantHistoryAppService.GetFundingHistoryAsync(id);
        FundingHistoryForm = new FundingHistoryModalViewModel
        {
            ApplicantId = record.ApplicantId ?? Guid.Empty,
            GrantCategory = record.GrantCategory,
            FundingYear = record.FundingYear,
            RenewedFunding = record.RenewedFunding,
            ApprovedAmount = record.ApprovedAmount,
            ReconsiderationAmount = record.ReconsiderationAmount,
            TotalGrantAmount = record.TotalGrantAmount,
            FundingNotes = record.FundingNotes
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var dto = new CreateUpdateFundingHistoryDto
        {
            ApplicantId = FundingHistoryForm!.ApplicantId,
            GrantCategory = FundingHistoryForm.GrantCategory,
            FundingYear = FundingHistoryForm.FundingYear,
            RenewedFunding = FundingHistoryForm.RenewedFunding,
            ApprovedAmount = FundingHistoryForm.ApprovedAmount,
            ReconsiderationAmount = FundingHistoryForm.ReconsiderationAmount,
            TotalGrantAmount = FundingHistoryForm.TotalGrantAmount,
            FundingNotes = FundingHistoryForm.FundingNotes
        };

        await _applicantHistoryAppService.UpdateFundingHistoryAsync(Id, dto);
        return NoContent();
    }
}
