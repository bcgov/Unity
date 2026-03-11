using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IAuditHistoryRepository))]
public class AuditHistoryRepository : EfCoreRepository<GrantTenantDbContext, AuditHistory, Guid>, IAuditHistoryRepository
{
    public AuditHistoryRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<List<AuditHistory>> GetByApplicantIdAsync(Guid applicantId)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.AuditHistories
            .Where(x => x.ApplicantId == applicantId)
            .ToListAsync();
    }
}
