 using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Services;

namespace Unity.Flex.WorksheetInstances
{
    [Authorize]
    public class WorksheetInstanceAppService(IWorksheetInstanceRepository worksheetInstanceRepository, WorksheetsManager worksheetsManager) : FlexAppService, IWorksheetInstanceAppService
    {
        public virtual async Task<WorksheetInstanceDto> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, Guid? worksheetId, string uiAnchor)
        {
            if (worksheetId == null)
                return ObjectMapper.Map<WorksheetInstance?, WorksheetInstanceDto>(await worksheetInstanceRepository.GetByCorrelationAnchorAsync(correlationId, correlationProvider, uiAnchor, true));
            else
                return ObjectMapper.Map<WorksheetInstance?, WorksheetInstanceDto>(await worksheetInstanceRepository.GetByCorrelationAnchorWorksheetAsync(correlationId, correlationProvider, worksheetId.Value, uiAnchor, true));
        }

        public virtual async Task<WorksheetInstanceDto> CreateAsync(CreateWorksheetInstanceDto dto)
        {
            var newWorksheet = new WorksheetInstance(Guid.NewGuid(), dto.WorksheetId, dto.CorrelationId, dto.CorrelationProvider, dto.SheetCorrelationId, dto.SheetCorrelationProvider, dto.CorrelationAnchor);
            var dbWorksheet = await worksheetInstanceRepository.InsertAsync(newWorksheet);

            return ObjectMapper.Map<WorksheetInstance, WorksheetInstanceDto>(dbWorksheet);
        }

        public virtual async Task UpdateAsync(PersistWorksheetIntanceValuesDto dto)
        {            
            await worksheetsManager.PersistWorksheetData(ObjectMapper.Map<PersistWorksheetIntanceValuesDto, PersistWorksheetIntanceValuesEto>(dto));
        }
    }
}