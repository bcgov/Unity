using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetAppService(IWorksheetRepository worksheetRepository) : FlexAppService, IWorksheetAppService
    {
        public virtual async Task<WorksheetDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Worksheet, WorksheetDto>(await worksheetRepository.GetAsync(id, true));
        }

        public virtual async Task<List<WorksheetDto>> GetListAsync()
        {
            var worksheets = await worksheetRepository.GetListOrderedAsync(true);

            return ObjectMapper.Map<List<Worksheet>, List<WorksheetDto>>(worksheets);
        }

        public virtual async Task<WorksheetDto> CreateAsync(CreateWorksheetDto dto)
        {
            var newWorksheet = new Worksheet(Guid.NewGuid(), dto.Name);
            var dbWorksheet = await worksheetRepository.InsertAsync(newWorksheet);

            return ObjectMapper.Map<Worksheet, WorksheetDto>(dbWorksheet);
        }

        public virtual async Task<WorksheetSectionDto> CreateSectionAsync(Guid id, CreateCustomFieldDto dto)
        {
            var worksheet = await worksheetRepository.GetAsync(id);
            var newWorksheetSection = new WorksheetSection(Guid.NewGuid(), dto.Name);
            worksheet.AddSection(newWorksheetSection);

            return ObjectMapper.Map<WorksheetSection, WorksheetSectionDto>(newWorksheetSection);
        }
    }
}
