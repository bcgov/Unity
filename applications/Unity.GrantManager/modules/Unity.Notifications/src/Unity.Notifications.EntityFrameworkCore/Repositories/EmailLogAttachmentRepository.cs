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
    public class EmailLogAttachmentRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider) : 
        EfCoreRepository<NotificationsDbContext, EmailLogAttachment, Guid>(dbContextProvider),
        IEmailLogAttachmentRepository
    {
        public async Task<List<EmailLogAttachment>> GetByEmailLogIdAsync(Guid emailLogId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => x.EmailLogId == emailLogId).ToListAsync();
        }

        public async Task<List<EmailLogAttachment>> GetByTemplateIdAsync(Guid templateId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => x.TemplateId == templateId).ToListAsync();
        }
    }
}
