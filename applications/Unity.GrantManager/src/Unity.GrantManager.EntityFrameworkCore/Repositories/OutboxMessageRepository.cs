using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Messaging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IOutboxMessageRepository))]
public class OutboxMessageRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider)
    : EfCoreRepository<GrantManagerDbContext, OutboxMessage, Guid>(dbContextProvider), IOutboxMessageRepository
{
    public async Task<List<OutboxMessage>> GetPendingAsync(string source, int maxCount = 10)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Where(m => m.Source == source && m.Status == MessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task<int> DeleteProcessedOlderThanAsync(DateTime cutoffDate)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.OutboxMessages
            .Where(m => (m.Status == MessageStatus.Processed || m.Status == MessageStatus.Failed)
                        && m.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync();
    }
}
