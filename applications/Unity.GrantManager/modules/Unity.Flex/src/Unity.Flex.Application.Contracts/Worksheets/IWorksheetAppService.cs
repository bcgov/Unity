using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Worksheets
{
    public interface IWorksheetAppService : IApplicationService
    {
        Task<WorksheetDto> GetAsync(Guid id);
        Task<WorksheetDto> CreateAsync(CreateWorksheetDto dto);
        Task<WorksheetSectionDto> CreateSectionAsync(Guid id, CreateCustomFieldDto dto);
        Task<List<WorksheetDto>> GetListAsync();
    }
}
