using Microsoft.AspNetCore.Mvc;
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

        public async Task OnGetAsync()
        {            
            SelectedOption = "SyncWorksheetFields"; // Default value
            await Task.CompletedTask;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var selectedOption = SelectedOption;

            switch (selectedOption)
            {
                case "SyncWorksheetFields":
                    await worksheetFieldsSyncService.SyncFields();
                    break;
                case "SyncWorksheetData":
                    await worksheetFieldsSyncService.SyncData();
                    break;
                case "SyncScoresheetFields":
                    await scoresheetFieldsSyncService.SyncQuestions();
                    break;
                case "SyncScoresheetData":
                    await scoresheetFieldsSyncService.SyncAnswers();
                    break;
                case "SyncSubmissionFields":
                    await formVersionReportingFieldsGeneratorService.SyncFormVersionFields();
                    break;
                case "SyncSubmissionData":
                    await formVersionReportingFieldsGeneratorService.SyncFormSubmissionData();
                    break;

            }

            return NoContent();
        }
    }
}
