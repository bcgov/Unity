using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public virtual async Task<WorksheetDto?> GetByUiAnchorAsync(string uiAnchor)
        {
            return ObjectMapper.Map<Worksheet?, WorksheetDto?>(await worksheetRepository.GetByUiAnchorAsync(uiAnchor, true));
        }

        public virtual async Task<List<WorksheetDto>> GetListAsync()
        {
            var worksheets = await worksheetRepository.GetListOrderedAsync(true);

            return ObjectMapper.Map<List<Worksheet>, List<WorksheetDto>>(worksheets);
        }

        public virtual async Task<WorksheetDto> CreateAsync(CreateWorksheetDto dto)
        {
            var newWorksheet = new Worksheet(Guid.NewGuid(), dto.Name, dto.UIAnchor);

            foreach (var section in dto.Sections.OrderBy(s => s.Order))
            {
                newWorksheet.AddSection(new WorksheetSection(Guid.NewGuid(), section.Name));

                foreach (var field in section.Fields)
                {
                    newWorksheet.Sections[^1].AddField(new CustomField(Guid.NewGuid(), field.Name, field.Label, field.Type));
                }
            }

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
