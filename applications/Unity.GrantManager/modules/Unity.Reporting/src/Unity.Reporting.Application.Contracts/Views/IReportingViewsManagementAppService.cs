using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Reporting.Views
{    
    public interface IReportingViewsManagementAppService : IApplicationService
    {
        public Task<SyncReportingViewsResult> SyncSubmissionViewsAndFields();
        public Task<SyncReportingViewsDataResult> SyncSubmissionData();
        public Task<SyncReportingViewsResult> SyncWorksheetViewsAndFields();
        public Task<SyncReportingViewsDataResult> SyncWorksheetData();
        public Task<SyncReportingViewsResult> SyncScoresheetViewsAndFields();
        public Task<SyncReportingViewsDataResult> SyncScoresheetData();
    }
}
