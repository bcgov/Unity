using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting
{
    public interface IScoresheetReportingFieldsGeneratorAppService : IApplicationService
    {
        Task Generate(Guid scoresheetId);
        Task Sync();
    }
}
