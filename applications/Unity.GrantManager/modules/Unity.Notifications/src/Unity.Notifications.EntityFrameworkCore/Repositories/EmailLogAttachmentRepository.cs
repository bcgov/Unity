using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EntityFrameworkCore;
using Unity.Notifications.Emails;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.Notifications.Repositories
{
    public class EmailLogAttachmentRepository : EfCoreRepository<NotificationsDbContext, EmailLogAttachment, Guid>,
        IEmailLogAttachmentRepository
    {
        public EmailLogAttachmentRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<List<EmailLogAttachment>> GetByEmailLogIdAsync(Guid emailLogId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => x.EmailLogId == emailLogId).ToListAsync();
        }
    }
}
