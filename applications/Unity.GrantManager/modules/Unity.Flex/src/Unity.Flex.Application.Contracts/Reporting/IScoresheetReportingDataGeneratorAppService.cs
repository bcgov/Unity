using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting
{
    public interface IScoresheetReportingDataGeneratorAppService : IApplicationService
    {
        Task Generate(Guid scoresheetInstanceId);
        Task Sync();
    }
}
