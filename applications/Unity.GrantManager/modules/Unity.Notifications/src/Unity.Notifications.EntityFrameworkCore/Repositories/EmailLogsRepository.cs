using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EntityFrameworkCore;
using Unity.Notifications.Emails;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;


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

        public override async Task<IQueryable<EmailLog>> WithDetailsAsync()
        {
            // Uses the extension method defined above
            return (await GetQueryableAsync());
        }
    }
}
