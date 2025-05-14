using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting
{
    public interface IWorksheetReportingFieldsSyncAppService : IApplicationService
    {
        Task SyncFields(Guid? tenantId);
        Task SyncData(Guid? tenantId);
    }
}
