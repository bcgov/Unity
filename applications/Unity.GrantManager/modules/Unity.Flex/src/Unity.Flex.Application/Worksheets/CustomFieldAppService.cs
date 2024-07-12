using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp.Domain.Entities;
using System.Linq;
using Unity.Flex.Worksheets.Definitions;

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
    }
}
