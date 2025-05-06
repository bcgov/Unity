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
public class ApplicationRepository : EfCoreRepository<GrantTenantDbContext, Application, Guid>, IApplicationRepository
{
    public ApplicationRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<List<IGrouping<Guid, Application>>> WithFullDetailsGroupedAsync(int skipCount, int maxResultCount, string? sorting = null)
    {
        var query = (await GetQueryableAsync())
            .AsNoTracking()
            .Include(s => s.ApplicationStatus)
            .Include(s => s.ApplicationForm)
            .Include(s => s.ApplicationTags)
            .Include(s => s.Owner)
            .Include(s => s.ApplicationAssignments!)
                .ThenInclude(t => t.Assignee)
            .Include(s => s.Applicant)
            .Include(s => s.ApplicantAgent);

        if (!string.IsNullOrEmpty(sorting))
        {
            query.OrderBy(sorting);
        }

        var groupBy = query
           .OrderBy(s => s.Id)
           .GroupBy(s => s.Id)
           .AsEnumerable()
           .Skip(skipCount)
           .Take(maxResultCount)
           .ToList();

        return groupBy;
    }

    public async Task<Application> WithBasicDetailsAsync(Guid id)
    {
        return await (await GetQueryableAsync())
          .AsNoTracking()
          .Include(s => s.Applicant)
            .ThenInclude(s => s.ApplicantAddresses)
          .Include(s => s.ApplicantAgent)
          .Include(s => s.ApplicationStatus)
          .FirstAsync(s => s.Id == id);
    }

    public async Task<List<Application>> GetListByIdsAsync(Guid[] ids)
    {
        return await (await GetQueryableAsync())
            .AsNoTracking()
            .Include(s => s.ApplicationStatus)
            .Include(s => s.Applicant)
            .Include(s => s.ApplicationForm)
            .Where(s => ids.Contains(s.Id))
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

    public async Task<Application?> GetWithFullDetailsByIdAsync(Guid id)
    {
        return await (await GetQueryableAsync())
            .Include(a => a.ApplicationStatus)
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationTags)
            .Include(a => a.Owner)
            .Include(a => a.ApplicationAssignments!)
                .ThenInclude(aa => aa.Assignee)
            .Include(a => a.Applicant)
            .Include(a => a.ApplicantAgent)
            .AsNoTracking()                 // read?only; drop this line if you need tracking
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}
