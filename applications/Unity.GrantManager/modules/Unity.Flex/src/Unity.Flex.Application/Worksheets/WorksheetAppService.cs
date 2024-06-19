using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetAppService(IWorksheetRepository worksheetRepository) : FlexAppService, IWorksheetAppService
    {
        public virtual async Task<WorksheetDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<Worksheet, WorksheetDto>(await worksheetRepository.GetAsync(id, true));
        }

        public virtual async Task<List<WorksheetDto>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider)
        {
            var worksheets = await worksheetRepository.GetListOrderedAsync(correlationId, correlationProvider, true);

            return ObjectMapper.Map<List<Worksheet>, List<WorksheetDto>>(worksheets);
        }

        public virtual async Task<WorksheetDto?> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, string uiAnchor)
        {
            var worksheet = await worksheetRepository.GetByCorrelationAnchorAsync(correlationId, correlationProvider, uiAnchor, true);

            if (worksheet == null) return null;

            return ObjectMapper.Map<Worksheet, WorksheetDto>(worksheet);
        }

        public virtual async Task<WorksheetDto> CreateAsync(CreateWorksheetDto dto)
        {
            var worksheetName = dto.Name.SanitizeWorksheetName();
            var existingWorksheet = await worksheetRepository.GetByNameAsync(worksheetName, false);

            if (existingWorksheet != null)
            {
                throw new BusinessException("Cannot have duplicate worksheet names");
            }

            var newWorksheet = new Worksheet(Guid.NewGuid(), worksheetName, dto.Title, dto.UIAnchor);

            foreach (var section in dto.Sections.OrderBy(s => s.Order))
            {
                newWorksheet.AddSection(new WorksheetSection(Guid.NewGuid(), section.Name));

                foreach (var field in section.Fields)
                {
                    newWorksheet
                        .Sections[^1]
                        .AddField(new CustomField(Guid.NewGuid(),
                            field.Name,
                            newWorksheet.Name,
                            field.Label,
                            field.Type,
                            field.Definition));
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

        public virtual async Task<List<WorksheetDto>> GetListAsync()
        {            
            return ObjectMapper.Map<List<Worksheet>, List<WorksheetDto>>(await worksheetRepository.GetListAsync(true));
        }
    }
}
