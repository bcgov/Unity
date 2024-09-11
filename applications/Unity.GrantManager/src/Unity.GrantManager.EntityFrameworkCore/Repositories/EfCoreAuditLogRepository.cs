using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Volo.Abp.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Volo.Abp.Identity;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Repositories
{
    [ExposeServices(typeof(IExtendedAuditLogRepository))]
    public class ExtendedEfCoreAuditLogRepository(IDbContextProvider<IAuditLoggingDbContext> dbContextProvider,
        IIdentityUserRepository identityUserRepository) : EfCoreAuditLogRepository(dbContextProvider), IExtendedAuditLogRepository
    {
        public virtual async Task<List<EntityChangeWithUsername>> GetEntityChangeByTypeWithUsernameAsync(Guid? entityId, List<string> entityTypeFullNames, CancellationToken cancellationToken)
        {
            List<EntityChangeWithUsername> entities = [];
            var dbSet = await GetDbSetAsync();
            var dqQuery = dbSet.AsNoTracking().IncludeDetails().Where(x => x.EntityChanges.All(y => entityTypeFullNames.Contains(y.EntityTypeFullName)));

            if (entityId != null && entityId != Guid.Empty)
            {
                dqQuery = dbSet.AsNoTracking().IncludeDetails().Where(x => x.EntityChanges.Any(y => y.EntityId == entityId.ToString()));
            }
            var auditLogs = await dqQuery.Distinct().ToListAsync(GetCancellationToken(cancellationToken));

            foreach (var auditLog in auditLogs)
            {
                foreach (var entityChange in auditLog.EntityChanges)
                {
                    string userName = "";
                    if (Guid.TryParse(auditLog.UserId.ToString(), out Guid userId))
                    {
                        userName = await ResolveUsername(userId);
                    }
                    entities.Add(
                        new EntityChangeWithUsername()
                        {
                            UserName = userName,
                            EntityChange = entityChange
                        }
                    );
                }
            }

            return entities;
        }

        private async Task<string> ResolveUsername(Guid userId)
        {
            var user = await identityUserRepository.GetAsync(userId);
            return user != null ? $"{user.Name} {user.Surname}" : string.Empty;
        }
    }
}
