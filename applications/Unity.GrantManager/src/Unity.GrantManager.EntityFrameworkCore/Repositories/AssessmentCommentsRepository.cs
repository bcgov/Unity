using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Unity.GrantManager.Comments;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ICommentsRepository<AssessmentComment>))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
// This pattern is an implementation ontop of ABP framework, will not change this
public class AssessmentCommentsRepository : EfCoreRepository<GrantTenantDbContext, AssessmentComment, Guid>, ICommentsRepository<AssessmentComment>
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
{
    public AssessmentCommentsRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<List<AssessmentComment>> GetListAsync(int skipCount, int maxResultCount, string sorting, string filter)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .WhereIf(
                !filter.IsNullOrWhiteSpace(),
                assessmentComment => assessmentComment.Comment.Contains(filter)
             )
            .OrderBy(sorting)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();
    }
}
