using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Reporting;
using Unity.Modules.Shared.Permissions;

namespace Unity.Reporting.Views
{
    [Authorize(IdentityConsts.ITAdminPolicy)]
    public class ReportingViewsManagementAppService(IScoresheetReportingFieldsGeneratorAppService reportingFieldsGeneratorAppService,
        IScoresheetReportingDataGeneratorAppService scoresheetReportingDataGeneratorAppService,
        IWorksheetReportingFieldsGeneratorAppService worksheetReportingFieldsGeneratorAppService,
        IWorksheetReportingDataGeneratorAppService worksheetReportingDataGeneratorAppService) : ReportingAppService, IReportingViewsManagementAppService
    {

        // Submissions
        public Task<SyncReportingViewsResult> SyncSubmissionViewsAndFields()
        {
            throw new NotImplementedException();
        }

        public Task<SyncReportingViewsDataResult> SyncSubmissionData()
        {
            throw new NotImplementedException();
        }

        // Scoresheets
        public Task<SyncReportingViewsResult> SyncScoresheetViewsAndFields()
        {
            throw new NotImplementedException();
        }

        public Task<SyncReportingViewsDataResult> SyncScoresheetData()
        {
            throw new NotImplementedException();
        }

        // Worksheets
        public async Task<SyncReportingViewsResult> SyncWorksheetViewsAndFields()
        {
            // Look for any worksheet that is published, and does not have the Report View Field name populated
            List<Guid> worksheetIds = new List<Guid>();
            foreach (var worksheetId in worksheetIds)
            {

                // Generate the Reporting Fields for the worksheet
                await worksheetReportingFieldsGeneratorAppService.Generate(worksheetId);
            }

            throw new NotImplementedException();
        }

        public Task<SyncReportingViewsDataResult> SyncWorksheetData()
        {
            throw new NotImplementedException();
        }
    }
}
