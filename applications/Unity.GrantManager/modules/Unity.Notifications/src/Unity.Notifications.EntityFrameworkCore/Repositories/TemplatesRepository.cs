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
    public class TemplatesRepository(IDbContextProvider<NotificationsDbContext> dbContextProvider) : EfCoreRepository<NotificationsDbContext,EmailTemplate, Guid>(dbContextProvider), ITemplatesRepository
    {
        public async Task<EmailTemplate?> GetByIdAsync(Guid id, bool includeDetails = false)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<EmailTemplate?> LockActiveAsync(Guid id)
        {
            var dbContext = await GetDbContextAsync();
            if (dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                if (dbContext.Database.CurrentTransaction == null)
                {
                    throw new InvalidOperationException("Template row locking requires a database transaction.");
                }

                return await dbContext.EmailTemplates
                    .FromSqlInterpolated($"""SELECT * FROM "Notifications"."EmailTemplates" WHERE "Id" = {id} FOR UPDATE""")
                    .SingleOrDefaultAsync();
            }

            if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // SQLite has no SELECT FOR UPDATE; a write serializes access inside a transaction.
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"""UPDATE "EmailTemplates" SET "IsDeleted" = "IsDeleted" WHERE "Id" = {id}""");
                return await dbContext.EmailTemplates.SingleOrDefaultAsync(x => x.Id == id);
            }

            throw new NotSupportedException("Template row locking requires PostgreSQL or SQLite.");
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
