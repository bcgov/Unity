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
            if (applicationId == Guid.Empty)
            {
                throw new ArgumentException(nameof(applicationId));
            }
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => x.ApplicationId == applicationId).ToListAsync();
        }

        public async Task<List<EmailLog>> GetByApplicantIdAsync(Guid applicantId)
        {
            if (applicantId == Guid.Empty)
            {
                throw new ArgumentException(nameof(applicantId));
            }
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => x.ApplicantId == applicantId && x.ApplicationId == Guid.Empty).ToListAsync();
        }

        public async Task<List<EmailLog>> GetByApplicationIdsAndStatusAsync(List<Guid> applicationIds, string status)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => applicationIds.Contains(x.ApplicationId) && x.Status == status).ToListAsync();
        }

        public override async Task<IQueryable<EmailLog>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync());
        }
    }
}
