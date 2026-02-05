using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
public class ApplicationRepository
    : EfCoreRepository<GrantTenantDbContext, Application, Guid>,
      IApplicationRepository
{
    private static readonly TimeZoneInfo VancouverTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    public ApplicationRepository(
        IDbContextProvider<GrantTenantDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    /// <summary>
    /// Converts Vancouver local date range to UTC range (inclusive)
    /// </summary>
    private static (DateTime? FromUtc, DateTime? ToUtc) ConvertToUtcRange(
        DateTime? fromLocal,
        DateTime? toLocal)
    {
        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        if (fromLocal.HasValue)
        {
            var localFrom = DateTime.SpecifyKind(
                fromLocal.Value,
                DateTimeKind.Unspecified);

            fromUtc = TimeZoneInfo.ConvertTimeToUtc(
                localFrom,
                VancouverTimeZone);
        }

        if (toLocal.HasValue)
        {
            // End of local day (23:59:59.9999999)
            var localToEndOfDay = DateTime.SpecifyKind(
                toLocal.Value.Date.AddDays(1).AddTicks(-1),
                DateTimeKind.Unspecified);

            toUtc = TimeZoneInfo.ConvertTimeToUtc(
                localToEndOfDay,
                VancouverTimeZone);
        }

        return (fromUtc, toUtc);
    }

    /// <summary>
    /// Base query with all required includes
    /// </summary>
    private async Task<IQueryable<Application>> BuildBaseQueryAsync()
    {
        return (await GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationStatus)
            .Include(a => a.Applicant)
            .Include(a => a.ApplicantAgent)
            .Include(a => a.ApplicationTags!)
                .ThenInclude(x => x.Tag)
            .Include(a => a.Owner)
            .Include(a => a.ApplicationAssignments!)
                .ThenInclude(aa => aa.Assignee);
    }

    public async Task<Application> WithBasicDetailsAsync(Guid id)
    {
        var application = await (await GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.Applicant)
                .ThenInclude(a => a.ApplicantAddresses)
            .Include(a => a.ApplicantAgent)
            .Include(a => a.ApplicationStatus)
            .FirstAsync(a => a.Id == id);

        if (application.Applicant?.ApplicantAddresses != null)
        {
            application.Applicant.ApplicantAddresses =
                new Collection<ApplicantAddress>(
                    application.Applicant.ApplicantAddresses
                        .Where(addr => addr.ApplicationId == id)
                        .ToList());
        }

        return application;
    }

    public async Task<Application?> GetWithFullDetailsByIdAsync(Guid id)
    {
        return await (await GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationStatus)
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationTags)
            .Include(a => a.Owner)
            .Include(a => a.ApplicationAssignments!)
                .ThenInclude(aa => aa.Assignee)
            .Include(a => a.Applicant)
            .Include(a => a.ApplicantAgent)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Application>> GetListByIdsAsync(Guid[] ids)
    {
        return await (await GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationStatus)
            .Include(a => a.Applicant)
            .Include(a => a.ApplicationForm)
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();
    }

    public override async Task<IQueryable<Application>> WithDetailsAsync()
    {
        return (await GetQueryableAsync()).IncludeDetails();
    }

    public async Task<long> GetCountAsync(
        DateTime? submittedFromDate,
        DateTime? submittedToDate)
    {
        var query = await BuildBaseQueryAsync();
        var (fromUtc, toUtc) = ConvertToUtcRange(
            submittedFromDate,
            submittedToDate);

        if (fromUtc.HasValue)
            query = query.Where(a => a.SubmissionDate >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(a => a.SubmissionDate <= toUtc.Value);

        return await query.LongCountAsync();
    }

    public async Task<List<Application>> WithFullDetailsAsync(
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        DateTime? submittedFromDate = null,
        DateTime? submittedToDate = null,
        string? searchTerm = null)
    {
        var query = await BuildBaseQueryAsync();
        var (fromUtc, toUtc) = ConvertToUtcRange(
            submittedFromDate,
            submittedToDate);

        if (fromUtc.HasValue)
            query = query.Where(a => a.SubmissionDate >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(a => a.SubmissionDate <= toUtc.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(a =>
                a.ProjectName.Contains(searchTerm) ||
                a.ReferenceNo.Contains(searchTerm));

        query = ApplySorting(query, sorting);

        return await query
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();
    }

    public async Task<List<Application>> GetByApplicantIdAsync(Guid applicantId)
    {
        var query = await BuildBaseQueryAsync();

        return await query
            .Where(a => a.ApplicantId == applicantId)
            .OrderByDescending(a => a.SubmissionDate)
            .ToListAsync();
    }

    private static IQueryable<Application> ApplySorting(
        IQueryable<Application> query,
        string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
            return query.OrderBy(a => a.SubmissionDate);

        var sortingFields = sorting
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !f.StartsWith("rowCount", StringComparison.OrdinalIgnoreCase))
            .Select(MapSortingField)
            .Where(f => f != null)
            .ToArray();

        if (sortingFields.Length > 0)
        {
            var sortingExpression = string.Join(",", sortingFields);
            try
            {
                return query.OrderBy(sortingExpression);
            }
            catch
            {
                return query.OrderBy(a => a.SubmissionDate);
            }
        }

        return query.OrderBy(a => a.SubmissionDate);
    }

    private static string? MapSortingField(string field)
    {
        if (field.StartsWith("status", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "status",
                "ApplicationStatus.InternalStatus",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("category", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "category",
                "ApplicationForm.Category",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("assignees", StringComparison.OrdinalIgnoreCase))
        {
            var parts = field.Split(' ', 2);
            return parts.Length == 2
                ? $"ApplicationAssignments.Count() {parts[1]}"
                : "ApplicationAssignments.Count()";
        }

        if (field.StartsWith("subStatusDisplayValue", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "subStatusDisplayValue",
                "SubStatus",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("applicationTag", StringComparison.OrdinalIgnoreCase))
        {
            var parts = field.Split(' ', 2);
            return parts.Length == 2
                ? $"ApplicationTags.FirstOrDefault().Text {parts[1]}"
                : "ApplicationTags.FirstOrDefault().Text";
        }

        if (field.StartsWith("organizationType", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "organizationType",
                "Applicant.OrganizationType",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("organizationName", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "organizationName",
                "Applicant.OrgName",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("businessNumber", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "businessNumber",
                "Applicant.BusinessNumber",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactFullName", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactFullName",
                "ApplicantAgent.Name",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactTitle", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactTitle",
                "ApplicantAgent.Title",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactEmail", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactEmail",
                "ApplicantAgent.Email",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactBusinessPhone", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactBusinessPhone",
                "ApplicantAgent.Phone",
                StringComparison.OrdinalIgnoreCase);

        if (field.StartsWith("contactCellPhone", StringComparison.OrdinalIgnoreCase))
            return field.Replace(
                "contactCellPhone",
                "ApplicantAgent.Phone2",
                StringComparison.OrdinalIgnoreCase);

        return field;
    }
}
