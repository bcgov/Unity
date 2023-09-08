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

namespace Unity.GrantManager.Repositories
{

    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ICommentsRepository<ApplicationComment>))]
    public class ApplicationCommentsRepository : EfCoreRepository<GrantManagerDbContext, ApplicationComment, Guid>, ICommentsRepository<ApplicationComment>
    {
        public ApplicationCommentsRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<List<ApplicationComment>> GetListAsync(int skipCount, int maxResultCount, string sorting, string filter)
        {
            return await (await GetDbSetAsync())
                .WhereIf(
                    !filter.IsNullOrWhiteSpace(),
                    applicationComment => applicationComment.Comment.Contains(filter)
                 )
                .OrderBy(sorting)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync();
        }
    }
}

