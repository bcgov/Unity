using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Worksheets
{
    public interface IWorksheetAppService : IApplicationService
    {
        Task<WorksheetDto> GetAsync(Guid id);
        Task<List<WorksheetDto>> GetListAsync();
        Task<List<WorksheetDto>> GetListByCorrelationAsync(Guid correlationId, string correlationProvider);
        Task<WorksheetDto?> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, string uiAnchor);
        Task<WorksheetDto> CreateAsync(CreateWorksheetDto dto);
        Task<WorksheetSectionDto> CreateSectionAsync(Guid id, CreateSectionDto dto);
        Task<WorksheetDto> EditAsync(Guid id, EditWorksheetDto dto);
        Task<WorksheetDto> CloneAsync(Guid id);
        Task<bool> PublishAsync(Guid id);
        Task DeleteAsync(Guid id);
        Task ResequenceSectionsAsync(Guid id, uint oldIndex, uint newIndex);
        Task<bool> ExistsAsync(Guid worksheetId);
        Task<ExportWorksheetDto> ExportWorksheet(Guid worksheetId);
        Task ImportWorksheetAsync(WorksheetImportDto worksheetImportDto);
    }
}
