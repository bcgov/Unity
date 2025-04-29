using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting
{
    public interface IWorksheetReportingFieldsSyncAppService : IApplicationService
    {
        Task GenerateFields(Guid worksheetId);
        Task GenerateData(Guid worksheetInstanceId);
        Task SyncFields();
        Task SyncData();
    }
}
