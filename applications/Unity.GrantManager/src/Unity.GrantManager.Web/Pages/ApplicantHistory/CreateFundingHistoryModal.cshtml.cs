using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class CreateFundingHistoryModal : AbpPageModel
{
    [BindProperty]
    public FundingHistoryModalViewModel? FundingHistoryForm { get; set; }

    private readonly IApplicantHistoryAppService _applicantHistoryAppService;

    public CreateFundingHistoryModal(IApplicantHistoryAppService applicantHistoryAppService)
    {
        _applicantHistoryAppService = applicantHistoryAppService;
    }

    public void OnGet(Guid applicantId)
    {
        FundingHistoryForm = new FundingHistoryModalViewModel
        {
            ApplicantId = applicantId
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
            OneTimeConsideration = FundingHistoryForm.OneTimeConsideration,
            TotalGrantAmount = FundingHistoryForm.TotalGrantAmount,
            FundingNotes = FundingHistoryForm.FundingNotes
        };

        await _applicantHistoryAppService.CreateFundingHistoryAsync(dto);
        return NoContent();
    }
}
