using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Flex.EntityFrameworkCore.Repositories
{
    public class WorksheetInstanceRepository(IDbContextProvider<FlexDbContext> dbContextProvider) : EfCoreRepository<FlexDbContext, WorksheetInstance, Guid>(dbContextProvider), IWorksheetInstanceRepository
    {
        public async Task<WorksheetInstance?> GetByCorrelationAnchorWorksheetAsync(Guid correlationId,
            string correlationProvider,
            Guid worksheetId,
            string uiAnchor,
            bool includeDetails)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.IncludeDetails(includeDetails)
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId
                        && s.CorrelationProvider == correlationProvider
                        && s.WorksheetId == worksheetId
                        && s.UiAnchor == uiAnchor);
        }

        public async Task<List<WorksheetInstance>> GetByWorksheetCorrelationAsync(Guid worksheetId,
            string uiAnchor,
            Guid worksheetCorrelationId,
            string worksheetCorrelationProvider)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .Where(s => s.WorksheetId == worksheetId
                        && s.UiAnchor == uiAnchor
                        && s.WorksheetCorrelationId == worksheetCorrelationId
                        && s.WorksheetCorrelationProvider == worksheetCorrelationProvider)
                .ToListAsync();
        }

        public async Task<WorksheetInstance?> GetWithValuesAsync(Guid worksheetInstanceId)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .Include(wi => wi.Values)
                .FirstOrDefaultAsync(wi => wi.Id == worksheetInstanceId);
        }

        public async Task<bool> ExistsAsync(Guid worksheetId,
            Guid instanceCorrelationId,
            string instanceCorrelationProvider,
            Guid sheetCorrelationId,
            string sheetCorrelationProvider,
            string? uiAnchor)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .AnyAsync(s => s.WorksheetId == worksheetId
                    && s.CorrelationId == instanceCorrelationId
                    && s.CorrelationProvider == instanceCorrelationProvider
                    && s.WorksheetCorrelationId == sheetCorrelationId
                    && s.WorksheetCorrelationProvider == sheetCorrelationProvider
                    && s.UiAnchor == uiAnchor);
        }

        public async Task<bool> AnyByWorksheetAndFormVersionAsync(Guid worksheetId, Guid formVersionId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.AnyAsync(s => s.WorksheetId == worksheetId
                && s.WorksheetCorrelationId == formVersionId
                && s.WorksheetCorrelationProvider == CorrelationConsts.FormVersion);
        }

        public async Task<List<WorksheetInstance>> GetByCorrelationIdsAsync(IEnumerable<Guid> correlationIds, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();
            var ids = correlationIds.ToList();
            return await dbSet
                .Where(wi => ids.Contains(wi.CorrelationId) && wi.CorrelationProvider == correlationProvider)
                .ToListAsync();
        }

        public async Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationProviderAsync(string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(wi => wi.CorrelationProvider == correlationProvider)
                .Select(wi => wi.WorksheetId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Guid>> GetDistinctWorksheetIdsByCorrelationIdsAsync(IEnumerable<Guid> correlationIds, string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();
            var ids = correlationIds.ToList();
            return await dbSet
                .Where(wi => ids.Contains(wi.CorrelationId) && wi.CorrelationProvider == correlationProvider)
                .Select(wi => wi.WorksheetId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<WorksheetInstance>> GetPagedListByCorrelationProviderAsync(string correlationProvider, int skipCount, int maxResultCount)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(wi => wi.CorrelationProvider == correlationProvider)
                .OrderByDescending(wi => wi.CreationTime)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync();
        }

        public async Task<int> GetCountByCorrelationProviderAsync(string correlationProvider)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.CountAsync(wi => wi.CorrelationProvider == correlationProvider);
        }
    }
}
