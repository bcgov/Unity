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
[ExposeServices(typeof(IIssueTrackingRepository))]
public class IssueTrackingRepository : EfCoreRepository<GrantTenantDbContext, IssueTracking, Guid>, IIssueTrackingRepository
{
    public IssueTrackingRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<List<IssueTracking>> GetByApplicantIdAsync(Guid applicantId)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.IssueTrackings
            .Where(x => x.ApplicantId == applicantId)
            .ToListAsync();
    }
}
