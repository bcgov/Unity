using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using System.Net;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.AuditLogging;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Repositories
{
    public class EfCoreAuditLogRepository
        : EfCoreRepository<GrantManagerDbContext, AuditLog, Guid>,
            IEfCoreAuditLogRepository
    {
        private readonly IIdentityUserAppService _identityUserAppService;

        public EfCoreAuditLogRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider, IIdentityUserAppService identityUserAppService)
            : base(dbContextProvider) { 
            _identityUserAppService = identityUserAppService;
        }

        public override async Task<IQueryable<AuditLog>> WithDetailsAsync()
        {
            return (await GetQueryableAsync()).IncludeDetails();
        }

        public virtual async Task<EntityChange> GetEntityChange(
            Guid entityChangeId,
            CancellationToken cancellationToken = default
        )
        {
            var entityChange = await (await GetDbContextAsync())
                .Set<EntityChange>()
                .AsNoTracking()
                .IncludeDetails()
                .Where(x => x.Id == entityChangeId)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));

            if (entityChange == null)
            {
                throw new EntityNotFoundException(typeof(EntityChange));
            }

            return entityChange;
        }

        public virtual async Task<List<EntityChange>> GetEntityChangeListAsync(
            string? sorting = null,
            int maxResultCount = 50,
            int skipCount = 0,
            Guid? auditLogId = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            EntityChangeType? changeType = null,
            string? entityId = null,
            string? entityTypeFullName = null,
            bool includeDetails = false,
            CancellationToken cancellationToken = default
        )
        {
            string entityFullName = entityTypeFullName ?? "";
            var query = await GetEntityChangeListQueryAsync(
                auditLogId,
                startTime,
                endTime,
                changeType,
                entityId,
                entityFullName,
                includeDetails
            );

            return await query
                .OrderBy(
                    sorting.IsNullOrWhiteSpace() ? (nameof(EntityChange.ChangeTime) + " DESC") : sorting
                )
                .PageBy(skipCount, maxResultCount)
                .ToListAsync(GetCancellationToken(cancellationToken));
        }

        public virtual async Task<long> GetEntityChangeCountAsync(
            Guid? auditLogId = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            EntityChangeType? changeType = null,
            string? entityId = null,
            string? entityTypeFullName = null,
            CancellationToken cancellationToken = default
        )
        {
            string entityFullName = entityTypeFullName ?? "";
            var query = await GetEntityChangeListQueryAsync(
                auditLogId,
                startTime,
                endTime,
                changeType,
                entityId,
                entityFullName
            );

            var totalCount = await query.LongCountAsync(GetCancellationToken(cancellationToken));

            return totalCount;
        }

        public virtual async Task<List<EntityChangeWithUsername>> GetEntityChangeByTypeWithUsernameAsync(
                                                                        Guid? entityId,
                                                                        List<string> entityTypeFullNames,
                                                                        CancellationToken cancellationToken
                                                                        )
        {
            List<EntityChangeWithUsername> entities = new List<EntityChangeWithUsername>();
            var dbSet = await GetDbSetAsync();
            var dqQuery = dbSet.AsNoTracking().IncludeDetails().Where(x => x.EntityChanges.All(y => entityTypeFullNames.Contains(y.EntityTypeFullName)));

            if(entityId != null && entityId != Guid.Empty) {
                dqQuery = dbSet.AsNoTracking().IncludeDetails().Where(x => x.EntityChanges.Any(y => y.EntityId == entityId.ToString()));
            }
            var auditLogs = await dqQuery.Distinct().ToListAsync(GetCancellationToken(cancellationToken));

            foreach(var auditLog in auditLogs)
            {
                foreach (var entityChange in auditLog.EntityChanges)
                {
                    string userName = "";
                    if(Guid.TryParse(auditLog.UserId.ToString(), out Guid userId))
                    {
                        userName = await LookupUserName(userId);
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

        public async Task<string> LookupUserName(Guid userId)
        {
            var user = await _identityUserAppService.GetAsync(userId);
            return user != null ? $"{user.Name} {user.Surname}" : string.Empty;
        }

        public virtual async Task<EntityChangeWithUsername> GetEntityChangeWithUsernameAsync(
            Guid entityChangeId,
            CancellationToken cancellationToken = default
        )
        {
            var auditLog = await (await GetDbSetAsync())
                .AsNoTracking()
                .IncludeDetails()
                .Where(x => x.EntityChanges.Any(y => y.Id == entityChangeId))
                .FirstAsync(GetCancellationToken(cancellationToken));

            return new EntityChangeWithUsername()
            {
                EntityChange = auditLog.EntityChanges.First(x => x.Id == entityChangeId),
                UserName = auditLog.UserName,
            };
        }

        protected virtual async Task<IQueryable<EntityChange>> GetEntityChangeListQueryAsync(
            Guid? auditLogId = null,
            DateTime? startTime = null,
            DateTime? endTime = null,
            EntityChangeType? changeType = null,
            string? entityId = null,
            string entityTypeFullName = "",
            bool includeDetails = false
        )
        {
            return (await GetDbContextAsync())
                .Set<EntityChange>()
                .AsNoTracking()
                .IncludeDetails(includeDetails)
                .WhereIf(auditLogId.HasValue, e => e.AuditLogId == auditLogId)
                .WhereIf(startTime.HasValue, e => e.ChangeTime >= startTime)
                .WhereIf(endTime.HasValue, e => e.ChangeTime <= endTime)
                .WhereIf(changeType.HasValue, e => e.ChangeType == changeType)
                .WhereIf(!string.IsNullOrWhiteSpace(entityId), e => e.EntityId == entityId)
                .WhereIf(
                    !string.IsNullOrWhiteSpace(entityTypeFullName),
                    e => e.EntityTypeFullName.Contains(entityTypeFullName)
                );
        }

        public Task<List<AuditLog>> GetListAsync(string? sorting = null, int maxResultCount = 50, int skipCount = 0, DateTime? startTime = null, DateTime? endTime = null, string? httpMethod = null, string? url = null, Guid? userId = null, string? userName = null, string? applicationName = null, string? clientIpAddress = null, string? correlationId = null, int? maxExecutionDuration = null, int? minExecutionDuration = null, bool? hasException = null, HttpStatusCode? httpStatusCode = null, bool includeDetails = false, CancellationToken cancellationToken = default(CancellationToken)) {
            throw new NotImplementedException();
        }

        public Task<long> GetCountAsync(DateTime? startTime = null, DateTime? endTime = null, string? httpMethod = null, string? url = null, Guid? userId = null, string? userName = null, string? applicationName = null, string? clientIpAddress = null, string? correlationId = null, int? maxExecutionDuration = null, int? minExecutionDuration = null, bool? hasException = null, HttpStatusCode? httpStatusCode = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<DateTime, double>> GetAverageExecutionDurationPerDayAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<List<EntityChangeWithUsername>> GetEntityChangesWithUsernameAsync(string entityId, string entityTypeFullName, CancellationToken cancellationToken = default(CancellationToken))
        {

         throw new NotImplementedException(); 
        }
    }
}
