using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting
{
    public interface IFormsReportSyncServiceAppService : IApplicationService
    {
        Task GenerateFormVersionFields(Guid formVersionId);
        Task GenerateFormSubmissionData(Guid submissionId);
        Task SyncFormVersionFields();
        Task SyncFormSubmissionData();
    }
}
