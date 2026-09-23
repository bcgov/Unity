using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EntityFrameworkCore;
using Unity.Notifications.Emails;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using System.Collections.Generic;


namespace Unity.Notifications.Repositories
{
    public class EmailLogsRepository : EfCoreRepository<NotificationsDbContext, EmailLog, Guid>, IEmailLogsRepository
    {
        public EmailLogsRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<EmailLog?> GetByIdAsync(Guid id, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<EmailLog>> GetByApplicationIdAsync(Guid applicationId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => x.ApplicationId == applicationId).ToListAsync();
        }

        public async Task<List<EmailLog>> GetByApplicationIdsAndStatusAsync(List<Guid> applicationIds, string status)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => applicationIds.Contains(x.ApplicationId) && x.Status == status).ToListAsync();
        }

        public async Task RestoreAuditStampsAsync(Guid id, Guid? modifierId, DateTime? modificationTime, string concurrencyStamp)
        {
            var dbSet = await GetDbSetAsync();
            // Only restore if nobody else has changed the row since our system save - otherwise a
            // concurrent user edit's stamp would be silently overwritten with the stale system values.
            await dbSet.Where(e => e.Id == id && e.ConcurrencyStamp == concurrencyStamp)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.LastModifierId, modifierId)
                    .SetProperty(e => e.LastModificationTime, modificationTime));
        }

        public override async Task<IQueryable<EmailLog>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync());
        }
    }
}
