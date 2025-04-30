using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Flex.Reporting;
using Unity.GrantManager.Reporting;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Reporting.Web.Pages.ReportViewSync
{
    public class ReportViewSyncModalModel(IWorksheetReportingFieldsSyncAppService worksheetFieldsSyncService,
        IScoresheetReportingFieldsSyncAppService scoresheetFieldsSyncService,
        IFormsReportSyncServiceAppService formVersionReportingFieldsGeneratorService) : AbpPageModel
    {
        [BindProperty]
        public string? SelectedOption { get; set; }
        
        [BindProperty]
        public string? TenantId { get; set; }

        public async Task OnGetAsync()
        {
            SelectedOption = "SyncWorksheetFields"; // Default value
            await Task.CompletedTask;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var selectedOption = SelectedOption;
            Guid? tenantId = TenantId != null ? Guid.Parse(TenantId) : null;

            switch (selectedOption)
            {
                case "SyncWorksheetFields":
                    await worksheetFieldsSyncService.SyncFields(tenantId);
                    break;
                case "SyncWorksheetData":
                    await worksheetFieldsSyncService.SyncData(tenantId);
                    break;
                case "SyncScoresheetFields":
                    await scoresheetFieldsSyncService.SyncQuestions(tenantId);
                    break;
                case "SyncScoresheetData":
                    await scoresheetFieldsSyncService.SyncAnswers(tenantId);
                    break;
                case "SyncSubmissionFields":
                    await formVersionReportingFieldsGeneratorService.SyncFormVersionFields(tenantId);
                    break;
                case "SyncSubmissionData":
                    await formVersionReportingFieldsGeneratorService.SyncFormSubmissionData(tenantId);
                    break;

            }

            return NoContent();
        }
    }
}
