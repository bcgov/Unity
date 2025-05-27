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

    public async Task<List<IGrouping<Guid, Application>>> WithFullDetailsGroupedAsync(int skipCount, int maxResultCount, string? sorting = null, string? filter = null)
    {
        var query = await BuildBaseQueryAsync();

        // Apply filter
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(a =>
                a.ProjectName.Contains(filter) ||
                a.ReferenceNo.Contains(filter)
            );
        }
        // Apply sorting
        query = ApplySorting(query, sorting);

        var groupedResult = query
            .AsEnumerable()
            .GroupBy(s => s.Id)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToList();

        return groupedResult;
    }

    private async Task<IQueryable<Application>> BuildBaseQueryAsync()
    {
        return (await GetQueryableAsync())
            .AsNoTracking()
            .Include(s => s.ApplicationStatus)
            .Include(s => s.ApplicationForm)
            .Include(s => s.ApplicationTags)
            .Include(s => s.Owner)
            .Include(s => s.ApplicationAssignments!)
                .ThenInclude(t => t.Assignee)
            .Include(s => s.Applicant)
            .Include(s => s.ApplicantAgent)
            .AsQueryable();
    }

    private static IQueryable<Application> ApplySorting(IQueryable<Application> query, string? sorting)
    {
        if (string.IsNullOrEmpty(sorting))
        {
            return query;
        }

        var sortingFields = sorting
            .Split(',')
            .Select(f => f.Trim())
            .Where(f => !f.StartsWith("rowCount", StringComparison.OrdinalIgnoreCase))
            .Select(MapSortingField)
            .Where(f => f != null)
            .ToArray();

        if (sortingFields.Length > 0)
        {
            var sortingExpression = string.Join(",", sortingFields);
            query = query.OrderBy(sortingExpression);
        }

        return query;
    }

    private static string? MapSortingField(string field)
    {
        if (field.StartsWith("status ", StringComparison.OrdinalIgnoreCase) || field.Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("status", "ApplicationStatus.InternalStatus", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("category ", StringComparison.OrdinalIgnoreCase) || field.Equals("category", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("category", "ApplicationForm.Category", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("assignees ", StringComparison.OrdinalIgnoreCase) || field.Equals("assignees", StringComparison.OrdinalIgnoreCase))
        {
            var parts = field.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 ? $"ApplicationAssignments.Count() {parts[1]}" : "ApplicationAssignments.Count()";
        }
        if (field.StartsWith("totalPaidAmount ", StringComparison.OrdinalIgnoreCase) || field.Equals("totalPaidAmount", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        if (field.StartsWith("subStatusDisplayValue ", StringComparison.OrdinalIgnoreCase) || field.Equals("subStatusDisplayValue", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("subStatusDisplayValue", "SubStatus", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("applicationTag ", StringComparison.OrdinalIgnoreCase) || field.Equals("applicationTag", StringComparison.OrdinalIgnoreCase))
        {
            var parts = field.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 ? $"ApplicationTags.FirstOrDefault().Text {parts[1]}" : "ApplicationTags.FirstOrDefault().Text";
        }
        if (field.StartsWith("organizationType ", StringComparison.OrdinalIgnoreCase) || field.Equals("organizationType", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("organizationType", "Applicant.OrganizationType", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("organizationName ", StringComparison.OrdinalIgnoreCase) || field.Equals("organizationName", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("organizationName", "Applicant.OrgName", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("contactFullName ", StringComparison.OrdinalIgnoreCase) || field.Equals("contactFullName", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("contactFullName", "ApplicantAgent.Name", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("contactTitle ", StringComparison.OrdinalIgnoreCase) || field.Equals("contactTitle", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("contactTitle", "ApplicantAgent.Title", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("contactEmail ", StringComparison.OrdinalIgnoreCase) || field.Equals("contactEmail", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("contactEmail", "ApplicantAgent.Email", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("contactBusinessPhone ", StringComparison.OrdinalIgnoreCase) || field.Equals("contactBusinessPhone", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("contactBusinessPhone", "ApplicantAgent.Phone", StringComparison.OrdinalIgnoreCase);
        }
        if (field.StartsWith("contactCellPhone ", StringComparison.OrdinalIgnoreCase) || field.Equals("contactCellPhone", StringComparison.OrdinalIgnoreCase))
        {
            return field.Replace("contactCellPhone", "ApplicantAgent.Phone2", StringComparison.OrdinalIgnoreCase);
        }
        return field;
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
