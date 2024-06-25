using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
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
            var worksheet = await worksheetRepository.GetBySectionAsync(id, true) ?? throw new EntityNotFoundException();
            var section = worksheet.Sections.FirstOrDefault(s => s.Id == id) ?? throw new EntityNotFoundException();
            worksheet.UpdateSection(section, dto.Name.Trim());
            return ObjectMapper.Map<WorksheetSection, WorksheetSectionDto>(section);
        }

        public virtual async Task CreateCustomField(Guid id, CreateCustomFieldDto dto)
        {
            var worksheet = await worksheetRepository.GetBySectionAsync(id, true) ?? throw new EntityNotFoundException();
            var section = worksheet.Sections.First(s => s.Id == id);

            section.AddField(new CustomField(Guid.NewGuid(),
                dto.Name,
                worksheet.Name,
                dto.Label,
                dto.Type,
                dto.Definition));
        }
    }
}
