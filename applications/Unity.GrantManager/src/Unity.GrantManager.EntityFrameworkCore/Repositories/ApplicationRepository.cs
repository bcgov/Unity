using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IApplicationRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
// This pattern is an implementation ontop of ABP framework, will not change this
public class ApplicationRepository : EfCoreRepository<GrantManagerDbContext, Application, Guid>, IApplicationRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
{
    public ApplicationRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<List<Application>> GetListAsync(int skipCount, int maxResultCount, string sorting, string filter = "")
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .WhereIf(
                !filter.IsNullOrWhiteSpace(),
                application => application.ProjectName.Contains(filter)
             )
            .OrderBy(sorting)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();
    }

    public async Task<List<Application>> GetDetailsListAsync()
    {
        return await (await GetDbSetAsync())
            .IncludeDetails()
            .ToListAsync();
    }

    /// <summary>
    /// Include defined sub-collections
    /// </summary>
    /// <remarks>See Best Practice: https://docs.abp.io/en/abp/latest/Best-Practices/Entity-Framework-Core-Integration#repository-implementation</remarks>
    /// <returns></returns>
    public override async Task<IQueryable<Application>> WithDetailsAsync()
    {
        // Uses the extension method defined above
        return (await GetQueryableAsync()).IncludeDetails();
    }
}
