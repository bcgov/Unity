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
        public virtual async Task<WorksheetInstanceDto> GetByCorrelationAsync(Guid correlationId, string correlationProvider, string uiAnchor)
        {
            return ObjectMapper.Map<WorksheetInstance?, WorksheetInstanceDto>(await worksheetInstanceRepository.GetByCorrelationAsync(correlationId, correlationProvider, uiAnchor, true));
        }

        public virtual async Task<WorksheetInstanceDto> CreateAsync(CreateWorksheetInstanceDto dto)
        {
            var newWorksheet = new WorksheetInstance(Guid.NewGuid(), dto.WorksheetId, dto.CorrelationId, dto.CorrelationProvider, dto.CorrelationAnchor);
            var dbWorksheet = await worksheetInstanceRepository.InsertAsync(newWorksheet);

            return ObjectMapper.Map<WorksheetInstance, WorksheetInstanceDto>(dbWorksheet);
        }

        public virtual async Task UpdateAsync(PersistWorksheetIntanceValuesDto dto)
        {            
            await worksheetsManager.PersistWorksheetData(ObjectMapper.Map<PersistWorksheetIntanceValuesDto, PersistWorksheetIntanceValuesEto>(dto));
        }
    }
}