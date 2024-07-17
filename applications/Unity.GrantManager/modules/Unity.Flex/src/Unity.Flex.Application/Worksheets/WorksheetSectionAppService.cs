using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class WorksheetSectionAppService(IWorksheetSectionRepository worksheetSectionRepository, IWorksheetRepository worksheetRepository) : FlexAppService, IWorksheetSectionAppService
    {
        public virtual async Task<WorksheetSectionDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<WorksheetSection, WorksheetSectionDto>(await worksheetSectionRepository.GetAsync(id, true));
        }

        public virtual async Task<WorksheetSectionDto> EditAsync(Guid id, EditSectionDto dto)
        {
            (Worksheet worksheet, WorksheetSection section) = await GetWorksheetAndSectionAsync(id);

            _ = worksheet.UpdateSection(section, dto.Name.Trim());
            return ObjectMapper.Map<WorksheetSection, WorksheetSectionDto>(section);
        }

        public virtual async Task<CustomFieldDto> CreateCustomFieldAsync(Guid id, CreateCustomFieldDto dto)
        {
            (Worksheet worksheet, WorksheetSection section) = await GetWorksheetAndSectionAsync(id);
            if (worksheet.Published) { throw new UserFriendlyException("Cannot add fields to a published worksheet"); }

            var customField = (new CustomField(Guid.NewGuid(),
                dto.Key,
                worksheet.Name,
                dto.Label,
                dto.Type,
                dto.Definition));

            section.AddField(customField);

            return ObjectMapper.Map<CustomField, CustomFieldDto>(customField);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            (Worksheet worksheet, WorksheetSection section) = await GetWorksheetAndSectionAsync(id);
            worksheet.RemoveSection(section);
        }

        public virtual async Task ResequenceCustomFieldsAsync(Guid id, uint oldIndex, uint newIndex)
        {
            if (oldIndex == newIndex) return;
            (Worksheet _, WorksheetSection section) = await GetWorksheetAndSectionAsync(id);

            var fields = section.Fields.OrderBy(s => s.Order).ToList();
            var movedField = fields[(int)oldIndex];
            movedField.SetOrder(newIndex + 1);

            if (oldIndex < newIndex)
            {
                foreach (var field in fields[(int)oldIndex..((int)newIndex + 1)].Where(s => s.Id != movedField.Id))
                {
                    field.SetOrder(movedField.Order - 1);
                }
            }
            else if (oldIndex > newIndex)
            {
                foreach (var field in fields[(int)newIndex..(int)oldIndex].Where(s => s.Id != movedField.Id))
                {
                    field.SetOrder(movedField.Order + 1);
                }
            }
        }

        private async Task<(Worksheet worksheet, WorksheetSection section)> GetWorksheetAndSectionAsync(Guid sectionId)
        {
            var worksheet = await worksheetRepository.GetBySectionAsync(sectionId, true) ?? throw new EntityNotFoundException();
            var section = worksheet.Sections.FirstOrDefault(s => s.Id == sectionId) ?? throw new EntityNotFoundException();

            return (worksheet, section);
        }
    }
}
