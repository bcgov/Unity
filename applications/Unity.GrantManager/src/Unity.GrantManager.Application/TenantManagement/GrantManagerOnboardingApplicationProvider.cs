#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.TenantManagement;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.TenantManagement;

[RemoteService(false)]
[ExposeServices(typeof(IOnboardingApplicationProvider))]
public class GrantManagerOnboardingApplicationProvider(
    IRepository<Application, Guid> applicationRepository,
    IRepository<ApplicationForm, Guid> applicationFormRepository,
    OnboardingApplicationManager onboardingApplicationManager)
    : IOnboardingApplicationProvider, ITransientDependency
{
    private async Task<IQueryable<Application>> BuildQueryAsync(string category)
    {
        return (await applicationRepository.GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationStatus)
            .Where(a => a.ApplicationForm.Category == category);
    }

    public async Task<PagedResultDto<OnboardingApplicationRecord>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string? sorting,
        string category,
        string? filter = null,
        IReadOnlyList<Guid>? globalDynamicMatchIds = null,
        IReadOnlyList<ColumnFilterDto>? staticColumnFilters = null,
        IReadOnlyList<Guid>? dynamicColumnMatchIds = null)
    {
        var query = await BuildQueryAsync(category);

        // Dynamic column filter IDs — empty list means active filters matched nothing
        if (dynamicColumnMatchIds != null)
        {
            if (dynamicColumnMatchIds.Count == 0)
                return new PagedResultDto<OnboardingApplicationRecord>(0, []);
            var dynSet = dynamicColumnMatchIds.ToHashSet();
            query = query.Where(a => dynSet.Contains(a.Id));
        }

        // Global text search: static fields OR pre-computed worksheet-matched IDs
        if (!string.IsNullOrWhiteSpace(filter))
        {
            if (globalDynamicMatchIds is { Count: > 0 })
            {
                var dynSet = globalDynamicMatchIds.ToHashSet();
                query = query.Where(a =>
                    a.ReferenceNo.Contains(filter) ||
                    a.ApplicationStatus.InternalStatus.Contains(filter) ||
                    (a.ApplicationForm.Category != null && a.ApplicationForm.Category.Contains(filter)) ||
                    dynSet.Contains(a.Id));
            }
            else
            {
                query = query.Where(a =>
                    a.ReferenceNo.Contains(filter) ||
                    a.ApplicationStatus.InternalStatus.Contains(filter) ||
                    (a.ApplicationForm.Category != null && a.ApplicationForm.Category.Contains(filter)));
            }
        }

        // Per-column static filters from the FilterRow
        if (staticColumnFilters != null)
        {
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
                               // Need to suppress this because EF Core does not support StringComparison
            foreach (var cf in staticColumnFilters.Where(cf => !string.IsNullOrWhiteSpace(cf.Value)))
            {
                var loweredValue = cf.Value.ToLowerInvariant();
                switch (cf.Name.ToLowerInvariant())
                {
                    case "submissionnumber":
                        query = query.Where(a => a.ReferenceNo.ToLower().Contains(loweredValue));
                        break;
                    case "status":
                        query = query.Where(a => a.ApplicationStatus.InternalStatus.ToLower().Contains(loweredValue));
                        break;
                    case "category":
                        query = query.Where(a => a.ApplicationForm.Category != null && a.ApplicationForm.Category.ToLower().Contains(loweredValue));
                        break;
                }
            }
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
        }

        var totalCount = await query.CountAsync();
        var ordered = ApplySorting(query, sorting);

        var items = await ordered
            .Skip(skipCount)
            .Take(maxResultCount)
            .Select(a => new OnboardingApplicationRecord
            {
                Id = a.Id,
                ReferenceNo = a.ReferenceNo,
                SubmissionDate = a.SubmissionDate,
                Status = a.ApplicationStatus.InternalStatus,
                Category = a.ApplicationForm.Category ?? string.Empty
            })
            .ToListAsync();

        return new PagedResultDto<OnboardingApplicationRecord>(totalCount, items);
    }

    public async Task<OnboardingApplicationRecord?> GetByIdAsync(Guid id)
    {
        var query = (await applicationRepository.GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationStatus);

        return await query
            .Where(a => a.Id == id)
            .Select(a => new OnboardingApplicationRecord
            {
                Id = a.Id,
                ReferenceNo = a.ReferenceNo,
                SubmissionDate = a.SubmissionDate,
                Status = a.ApplicationStatus.InternalStatus,
                Category = a.ApplicationForm.Category ?? string.Empty
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<Guid>> GetAllIdsAsync(string category)
    {
        var query = await BuildQueryAsync(category);
        return await query.Select(a => a.Id).ToListAsync();
    }

    public async Task CloseApplicationAsync(Guid applicationId)
    {
        await onboardingApplicationManager.TriggerAction(applicationId, GrantApplicationAction.Close);
    }

    public async Task<List<string>> GetAvailableCategoriesAsync()
    {
        var query = await applicationFormRepository.GetQueryableAsync();
        return await query
            .AsNoTracking()
            .Where(f => f.Category != null && f.Category != string.Empty)
            .Select(f => f.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    private static IQueryable<Application> ApplySorting(IQueryable<Application> query, string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
            return query.OrderByDescending(a => a.SubmissionDate);

        var parts = sorting.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var field = parts[0];
        var desc = parts.Length > 1 && parts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase);

        return field.ToLowerInvariant() switch
        {
            "referenceno" or "submissionnumber" => desc
                ? query.OrderByDescending(a => a.ReferenceNo)
                : query.OrderBy(a => a.ReferenceNo),
            "status" => desc
                ? query.OrderByDescending(a => a.ApplicationStatus.InternalStatus)
                : query.OrderBy(a => a.ApplicationStatus.InternalStatus),
            "submissiondate" => desc
                ? query.OrderByDescending(a => a.SubmissionDate)
                : query.OrderBy(a => a.SubmissionDate),
            "category" => desc
                ? query.OrderByDescending(a => a.ApplicationForm.Category)
                : query.OrderBy(a => a.ApplicationForm.Category),
            _ => query.OrderByDescending(a => a.SubmissionDate)
        };
    }
}
