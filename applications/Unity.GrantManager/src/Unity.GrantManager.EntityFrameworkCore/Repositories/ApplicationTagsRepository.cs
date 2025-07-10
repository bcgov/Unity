using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IApplicationTagsRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
// This pattern is an implementation ontop of ABP framework, will not change this
public class ApplicationTagsRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider)
    : EfCoreRepository<GrantTenantDbContext, ApplicationTags, Guid>(dbContextProvider), IApplicationTagsRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
{
    public virtual async Task<List<TagSummaryCount>> GetTagSummary()
    {
        var dbSet = await GetDbSetAsync();
        var results = await dbSet
                .AsNoTracking()
                .Include(x => x.Tag) // Ensure Tag is loaded
                .GroupBy(x => x.Tag)
                .Select(group => new TagSummaryCount(
                    group.Key,
                    group.Count()
                ))
                .ToListAsync();
        return results;
    }

    public override async Task<IQueryable<ApplicationTags>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).Include(x => x.Tag);
    }
}
