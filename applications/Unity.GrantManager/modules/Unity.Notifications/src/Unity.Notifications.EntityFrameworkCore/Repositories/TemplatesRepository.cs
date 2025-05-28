using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using System.Collections.Generic;
using Unity.Notifications.Templates;




namespace Unity.Notifications.Repositories
{
    public class TemplatesRepository : EfCoreRepository<NotificationsDbContext,EmailTemplate, Guid>, ITemplatesRepository
    {
        public TemplatesRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<EmailTemplate?> GetByIdAsync(Guid id, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<EmailTemplate>> GetByTenentIdAsync(Guid? tenentId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.Where(x => x.TenantId == tenentId).ToListAsync();
        }

        public async Task<EmailTemplate?> GetByNameAsync(string name)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet
                .FirstOrDefaultAsync(s => s.Name == name);
        }
    }
}
