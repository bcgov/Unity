using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Services;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.WorksheetInstances
{
    [Authorize]
    public class WorksheetInstanceAppService(IWorksheetInstanceRepository worksheetInstanceRepository, WorksheetsManager worksheetsManager) : FlexAppService, IWorksheetInstanceAppService
    {
        public virtual async Task<WorksheetInstanceDto> GetByCorrelationAnchorAsync(Guid correlationId, string correlationProvider, Guid worksheetId, string uiAnchor)
        {
            return ObjectMapper.Map<WorksheetInstance?, WorksheetInstanceDto>(await worksheetInstanceRepository.GetByCorrelationAnchorWorksheetAsync(correlationId, correlationProvider, worksheetId, uiAnchor, true));
        }

        public virtual async Task<WorksheetInstanceDto> CreateAsync(CreateWorksheetInstanceDto dto)
        {
            var newWorksheet = new WorksheetInstance(Guid.NewGuid(), dto.WorksheetId, dto.CorrelationId, dto.CorrelationProvider, dto.SheetCorrelationId, dto.SheetCorrelationProvider, dto.CorrelationAnchor, dto.ReportData);

            if (!string.IsNullOrEmpty(dto.CurrentValue)) { newWorksheet.SetValue(dto.CurrentValue); }

            var dbWorksheet = await worksheetInstanceRepository.InsertAsync(newWorksheet);

            return ObjectMapper.Map<WorksheetInstance, WorksheetInstanceDto>(dbWorksheet);
        }

        public virtual async Task UpdateAsync(PersistWorksheetIntanceValuesDto dto)
        {
            await worksheetsManager.PersistWorksheetData(ObjectMapper.Map<PersistWorksheetIntanceValuesDto, PersistWorksheetIntanceValuesEto>(dto));
        }

        public virtual async Task<List<WorksheetInstanceDataDto>> GetListByCorrelationIdsAsync(List<Guid> correlationIds, string correlationProvider)
        {
            var instances = await worksheetInstanceRepository.GetByCorrelationIdsAsync(correlationIds, correlationProvider);
            return instances.Select(wi => new WorksheetInstanceDataDto
            {
                CorrelationId = wi.CorrelationId,
                WorksheetId = wi.WorksheetId,
                CurrentValue = wi.CurrentValue
            }).ToList();
        }

        public virtual async Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationProviderAsync(string correlationProvider)
        {
            return await worksheetInstanceRepository.GetDistinctWorksheetIdsByCorrelationProviderAsync(correlationProvider);
        }

        public virtual async Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationIdsAsync(List<Guid> correlationIds, string correlationProvider)
        {
            return await worksheetInstanceRepository.GetDistinctWorksheetIdsByCorrelationIdsAsync(correlationIds, correlationProvider);
        }

        public virtual async Task<PagedResultDto<WorksheetInstanceDataDto>> GetPagedListByCorrelationProviderAsync(string correlationProvider, int skipCount, int maxResultCount)
        {
            var totalCount = await worksheetInstanceRepository.GetCountByCorrelationProviderAsync(correlationProvider);
            var instances = await worksheetInstanceRepository.GetPagedListByCorrelationProviderAsync(correlationProvider, skipCount, maxResultCount);
            var items = instances.Select(wi => new WorksheetInstanceDataDto
            {
                Id = wi.Id,
                CorrelationId = wi.CorrelationId,
                WorksheetId = wi.WorksheetId,
                CurrentValue = wi.CurrentValue,
                CreationTime = wi.CreationTime
            }).ToList();
            return new PagedResultDto<WorksheetInstanceDataDto>(totalCount, items);
        }

        public virtual async Task<WorksheetInstanceDataDto?> GetDataByIdAsync(Guid id)
        {
            var wi = await worksheetInstanceRepository.FindAsync(id);
            if (wi == null) return null;
            return new WorksheetInstanceDataDto
            {
                Id = wi.Id,
                CorrelationId = wi.CorrelationId,
                WorksheetId = wi.WorksheetId,
                CurrentValue = wi.CurrentValue,
                CreationTime = wi.CreationTime
            };
        }
    }
}