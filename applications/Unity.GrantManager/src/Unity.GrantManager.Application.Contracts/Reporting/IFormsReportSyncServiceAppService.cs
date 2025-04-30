using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Reporting
{
    public interface IFormsReportSyncServiceAppService : IApplicationService
    {
        Task SyncFormVersionFields(Guid? tenantId);
        Task SyncFormSubmissionData(Guid? tenantId);
    }
}
