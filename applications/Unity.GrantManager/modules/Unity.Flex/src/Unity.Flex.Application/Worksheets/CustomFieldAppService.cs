using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.Domain.Entities;
using System.Linq;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public class CustomFieldAppService(ICustomFieldRepository customFieldRepostitory,
        IWorksheetRepository worksheetRepository) : FlexAppService, ICustomFieldAppService
    {
        public virtual async Task<CustomFieldDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<CustomField, CustomFieldDto>(await customFieldRepostitory.GetAsync(id, true));
        }

        public virtual async Task<CustomFieldDto> EditAsync(Guid id, EditCustomFieldDto dto)
        {
            var field = await customFieldRepostitory.GetAsync(id) ?? throw new EntityNotFoundException();
            var worksheet = await worksheetRepository.GetBySectionAsync(field.SectionId, true) ?? throw new EntityNotFoundException();
            var worksheetField = worksheet.Sections.SelectMany(s => s.Fields).FirstOrDefault(s => s.Id == id) ?? throw new EntityNotFoundException();

            worksheetField.SetLabel(dto.Label);
            worksheetField.SetKey(dto.Key, worksheet.Name);
            worksheetField.SetType(dto.Type);
            worksheetField.SetDefinition(DefinitionResolver.Resolve(dto.Type, dto.Definition));
            
            return ObjectMapper.Map<CustomField, CustomFieldDto>(worksheetField);
        }

        public async Task DeleteAsync(Guid id)
        {
            var field = await customFieldRepostitory.GetAsync(id) ?? throw new EntityNotFoundException();
            var worksheet = await worksheetRepository.GetBySectionAsync(field.SectionId, true) ?? throw new EntityNotFoundException();
            var section = worksheet.Sections.FirstOrDefault(s => s.Id == field.SectionId) ?? throw new EntityNotFoundException();

            section.RemoveField(field);
        }

        public async Task MoveToSectionAsync(Guid fieldId, Guid targetSectionId, uint newIndex)
        {
            var field = await customFieldRepostitory.GetAsync(fieldId) ?? throw new EntityNotFoundException();
            if (field.SectionId == targetSectionId) return;

            var worksheet = await worksheetRepository.GetBySectionAsync(field.SectionId, true) ?? throw new EntityNotFoundException();
            if (worksheet.Published) throw new UserFriendlyException("Cannot move fields in a published worksheet.");

            var sourceSection = worksheet.Sections.FirstOrDefault(s => s.Id == field.SectionId) ?? throw new EntityNotFoundException();
            var targetSection = worksheet.Sections.FirstOrDefault(s => s.Id == targetSectionId) ?? throw new EntityNotFoundException();

            // Renumber source section, excluding the moving field
            var sourceFields = sourceSection.Fields.Where(f => f.Id != fieldId).OrderBy(f => f.Order).ToList();
            for (int i = 0; i < sourceFields.Count; i++)
                sourceFields[i].SetOrder((uint)(i + 1));

            // Make room in target section at the insertion point
            uint insertAt = Math.Min(newIndex + 1, (uint)(targetSection.Fields.Count + 1));
            foreach (var f in targetSection.Fields.Where(f => f.Order >= insertAt))
                f.SetOrder(f.Order + 1);

            // Update FK directly — EF Core change tracker issues a single UPDATE, avoids orphan-deletion
            field.SetOrder(insertAt);
            field.SectionId = targetSectionId;
        }
    }
}
