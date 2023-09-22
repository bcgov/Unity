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
public class AssessmentCommentsRepository : EfCoreRepository<GrantManagerDbContext, AssessmentComment, Guid>, ICommentsRepository<AssessmentComment>
{
    public AssessmentCommentsRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
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
