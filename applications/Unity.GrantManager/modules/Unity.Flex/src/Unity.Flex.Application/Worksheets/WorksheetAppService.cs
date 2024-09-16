using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Services;
using Unity.Flex.Domain.Settings;
using Unity.Flex.Domain.Utils;
using Unity.Flex.Domain.Worksheets;
using Volo.Abp;

namespace Unity.Flex.Worksheets
{
    [Authorize]
    public partial class WorksheetAppService(IWorksheetRepository worksheetRepository, WorksheetsManager worksheetsManager) : FlexAppService, IWorksheetAppService
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
            // move to domain manager class
            var worksheetName = dto.Name.SanitizeWorksheetName();
            var existingWorksheet = await worksheetRepository.GetByNameAsync(worksheetName, false);

            if (existingWorksheet != null)
            {
                throw new UserFriendlyException("Worksheet names must be unique");
            }

            var newWorksheet = new Worksheet(Guid.NewGuid(), worksheetName, dto.Title);

            foreach (var section in dto.Sections.OrderBy(s => s.Order))
            {
                newWorksheet.AddSection(new WorksheetSection(Guid.NewGuid(), section.Name));

                foreach (var field in section.Fields)
                {
                    newWorksheet
                        .Sections[^1]
                        .AddField(new CustomField(Guid.NewGuid(),
                            field.Key,
                            newWorksheet.Name,
                            field.Label,
                            field.Type,
                            field.Definition));
                }
            }

            var dbWorksheet = await worksheetRepository.InsertAsync(newWorksheet);

            return ObjectMapper.Map<Worksheet, WorksheetDto>(dbWorksheet);
        }

        public virtual async Task<WorksheetSectionDto> CreateSectionAsync(Guid id, CreateSectionDto dto)
        {
            var worksheet = await worksheetRepository.GetAsync(id, true);
            if (worksheet.Published) { throw new UserFriendlyException("Cannot add sections to a published worksheet"); }

            var newWorksheetSection = new WorksheetSection(Guid.NewGuid(), dto.Name);
            worksheet.AddSection(newWorksheetSection);

            return ObjectMapper.Map<WorksheetSection, WorksheetSectionDto>(newWorksheetSection);
        }

        public virtual async Task<List<WorksheetDto>> GetListAsync()
        {
            return ObjectMapper.Map<List<Worksheet>, List<WorksheetDto>>(await worksheetRepository.GetListAsync(true));
        }

        public virtual async Task<WorksheetDto> EditAsync(Guid id, EditWorksheetDto dto)
        {
            var worksheet = await worksheetRepository.GetAsync(id);
            worksheet.SetTitle(dto.Title);
            return ObjectMapper.Map<Worksheet, WorksheetDto>(worksheet);
        }

        public virtual async Task<WorksheetDto> CloneAsync(Guid id)
        {
            var worksheet = await worksheetsManager.CloneWorksheetAsync(id);
            return ObjectMapper.Map<Worksheet, WorksheetDto>(worksheet);
        }

        public virtual async Task<bool> PublishAsync(Guid id)
        {
            var worksheet = await worksheetRepository.GetAsync(id);
            _ = worksheet.SetPublished(true);
            return await Task.FromResult(true);
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await worksheetRepository.DeleteAsync(id);
        }

        public virtual async Task ResequenceSectionsAsync(Guid id, uint oldIndex, uint newIndex)
        {
            if (oldIndex == newIndex) return;
            var worksheet = await worksheetRepository.GetAsync(id);            

            var sections = worksheet.Sections.OrderBy(s => s.Order).ToList();
            var movedSection = sections[(int)oldIndex];
            movedSection.SetOrder(newIndex + 1);

            if (oldIndex < newIndex)
            {
                foreach (var field in sections[(int)oldIndex..((int)newIndex + 1)].Where(s => s.Id != movedSection.Id))
                {
                    field.SetOrder(movedSection.Order - 1);
                }
            }
            else if (oldIndex > newIndex)
            {
                foreach (var field in sections[(int)newIndex..(int)oldIndex].Where(s => s.Id != movedSection.Id))
                {
                    field.SetOrder(movedSection.Order + 1);
                }
            }
        }

        public async Task<bool> ExistsAsync(Guid worksheetId)
        {
            return await worksheetRepository.FindAsync(worksheetId, false) != null;
        }

        public async Task<ExportWorksheetDto> ExportWorksheet(Guid worksheetId)
        {
            var worksheet = await worksheetRepository.GetAsync(worksheetId, true);
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new WorksheetContractResolver()
            };
            var json = JsonConvert.SerializeObject(worksheet, settings);
            var byteArray = System.Text.Encoding.UTF8.GetBytes(json);

            return new ExportWorksheetDto { Content = byteArray, ContentType = "application/json", Name = "worksheet_"+worksheet.Title+"_"+worksheet.Name + ".json"};
        }

        public async Task ImportWorksheetAsync(WorksheetImportDto worksheetImportDto)
        {
            if (worksheetImportDto.Content == null || worksheetImportDto.Content.Length == 0)
            {
                throw new UserFriendlyException("No file content provided.");
            }

            var json = worksheetImportDto.Content;
            var worksheet = JsonConvert.DeserializeObject<Worksheet>(json, new JsonSerializerSettings
            {
                ContractResolver = new PrivateSetterContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            }) ?? throw new UserFriendlyException("Invalid JSON content.");
            string? name;

            var worksheets = await worksheetRepository.GetByNameStartsWithAsync(SheetParserFunctions.RemoveTrailingNumbers(worksheet.Name));
            var maxVersion = worksheets.Max(s => s.Version);
            var newVersion = maxVersion + 1;
            name = worksheet.Name.Replace($"-v{worksheet.Version}", $"-v{newVersion}");
            worksheet.SetVersion(newVersion);
            _ = worksheet.SetName(name);

            foreach(var section in worksheet.Sections)
            {
                foreach(var field in section.Fields)
                {
                    _ = field.UpdateFieldName(worksheet.Name);
                }
            }

            worksheet.SetPublished(false);
            await worksheetRepository.InsertAsync(worksheet);
        }
    }
}
