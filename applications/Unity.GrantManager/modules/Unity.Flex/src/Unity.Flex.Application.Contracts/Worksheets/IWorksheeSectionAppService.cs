using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Worksheets
{
    public interface IWorksheeSectionAppService : IApplicationService
    {
        Task CreateCustomField(Guid id, CreateCustomFieldDto dto);
        Task<WorksheetSectionDto> GetAsync(Guid id);
    }
}
