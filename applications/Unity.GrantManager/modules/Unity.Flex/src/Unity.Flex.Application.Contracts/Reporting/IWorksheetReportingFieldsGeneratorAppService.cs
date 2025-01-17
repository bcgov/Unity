using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting
{
    public interface IWorksheetReportingFieldsGeneratorAppService : IApplicationService
    {
        Task Generate(Guid worksheetId);
    }
}
