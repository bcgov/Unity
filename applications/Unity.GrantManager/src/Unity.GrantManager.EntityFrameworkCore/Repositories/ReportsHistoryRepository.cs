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
[ExposeServices(typeof(IReportsHistoryRepository))]
public class ReportsHistoryRepository : EfCoreRepository<GrantTenantDbContext, ReportsHistory, Guid>, IReportsHistoryRepository
{
    public ReportsHistoryRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<List<ReportsHistory>> GetByApplicantIdAsync(Guid applicantId)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.ReportsHistories
            .Where(x => x.ApplicantId == applicantId)
            .ToListAsync();
    }
}
