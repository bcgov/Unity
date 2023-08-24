using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IAssessmentCommentRepository))]
public class AssessmentCommentRepository : EfCoreRepository<GrantManagerDbContext, AssessmentComment, Guid>, IAssessmentCommentRepository
{
    public AssessmentCommentRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
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
