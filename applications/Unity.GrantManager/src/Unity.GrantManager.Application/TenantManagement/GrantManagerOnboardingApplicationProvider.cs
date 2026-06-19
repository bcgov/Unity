#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
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
    IRepository<ApplicationFormVersion, Guid> applicationFormVersionRepository,
    OnboardingApplicationManager onboardingApplicationManager)
    : IOnboardingApplicationProvider, ITransientDependency
{
    private async Task<IQueryable<Application>> BuildQueryAsync(string category)
    {
        return (await applicationRepository.GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationStatus)
            .Include(a => a.Applicant)
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

        // Global text search: static fields OR pre-computed worksheet-matched IDs OR core-field matches
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var effectiveMatchIds = await MergeCoreFieldGlobalMatchesAsync(category, filter, globalDynamicMatchIds);

            if (effectiveMatchIds is { Count: > 0 })
            {
                var dynSet = effectiveMatchIds.ToHashSet();
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
                    default:
                        query = ApplyCoreFieldFilter(query, cf.Name, loweredValue);
                        break;
                }
            }
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
        }

        var totalCount = await query.CountAsync();
        var ordered = ApplySorting(query, sorting);

        var entities = await ordered
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync();

        var coreFields = await GetMappedCoreFieldsAsync(category);
        var items = entities.Select(a => ToRecord(a, coreFields)).ToList();

        return new PagedResultDto<OnboardingApplicationRecord>(totalCount, items);
    }

    public async Task<OnboardingApplicationRecord?> GetByIdAsync(Guid id)
    {
        var query = (await applicationRepository.GetQueryableAsync())
            .AsNoTracking()
            .Include(a => a.ApplicationForm)
            .Include(a => a.ApplicationStatus)
            .Include(a => a.Applicant);

        var entity = await query.FirstOrDefaultAsync(a => a.Id == id);
        if (entity == null) return null;

        var coreFields = await GetMappedCoreFieldsAsync(entity.ApplicationForm.Category ?? string.Empty);
        return ToRecord(entity, coreFields);
    }

    private static OnboardingApplicationRecord ToRecord(Application a, IReadOnlyList<CoreFieldDefinition> coreFields) =>
        new()
        {
            Id = a.Id,
            ReferenceNo = a.ReferenceNo,
            SubmissionDate = a.SubmissionDate,
            Status = a.ApplicationStatus.InternalStatus,
            Category = a.ApplicationForm.Category ?? string.Empty,
            CoreFieldValues = coreFields.ToDictionary(f => f.Key, f => f.Selector(a))
        };

    public async Task<List<Guid>> GetAllIdsAsync(string category)
    {
        var query = await BuildQueryAsync(category);
        return await query.Select(a => a.Id).ToListAsync();
    }

    public async Task<List<Guid>> GetFormVersionIdsAsync(string category)
    {
        var versions = await GetCurrentPublishedVersionsAsync(category);
        return versions.Select(v => v.Id).ToList();
    }

    public async Task<List<OnboardingColumnDto>> GetMappedCoreFieldColumnsAsync(string category)
    {
        var coreFields = await GetMappedCoreFieldsAsync(category);
        return coreFields
            .Select(f => new OnboardingColumnDto { Key = f.Key, Label = f.Label, Type = f.Type, Selected = true })
            .ToList();
    }

    private async Task<List<CoreFieldDefinition>> GetMappedCoreFieldsAsync(string category)
    {
        var versions = await GetCurrentPublishedVersionsAsync(category);
        if (versions.Count == 0) return [];

        return OnboardingCoreFieldRegistry.Fields
            .Where(f => versions.Any(v => v.HasSubmissionHeaderMapping(f.Key)))
            .ToList();
    }

    private async Task<List<ApplicationFormVersion>> GetCurrentPublishedVersionsAsync(string category)
    {
        var formIds = await (await applicationFormRepository.GetQueryableAsync())
            .AsNoTracking()
            .Where(f => f.Category == category)
            .Select(f => f.Id)
            .ToListAsync();

        if (formIds.Count == 0) return [];

        var versions = await (await applicationFormVersionRepository.GetQueryableAsync())
            .AsNoTracking()
            .Where(v => formIds.Contains(v.ApplicationFormId) && v.Published)
            .ToListAsync();

        return versions
            .GroupBy(v => v.ApplicationFormId)
            .Select(g => g.OrderByDescending(v => v.Version).First())
            .ToList();
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

        switch (field.ToLowerInvariant())
        {
            case "referenceno" or "submissionnumber":
                return desc ? query.OrderByDescending(a => a.ReferenceNo) : query.OrderBy(a => a.ReferenceNo);
            case "status":
                return desc
                    ? query.OrderByDescending(a => a.ApplicationStatus.InternalStatus)
                    : query.OrderBy(a => a.ApplicationStatus.InternalStatus);
            case "submissiondate":
                return desc ? query.OrderByDescending(a => a.SubmissionDate) : query.OrderBy(a => a.SubmissionDate);
            case "category":
                return desc
                    ? query.OrderByDescending(a => a.ApplicationForm.Category)
                    : query.OrderBy(a => a.ApplicationForm.Category);
        }

        // Not a static column — check the core-field registry and sort via its EF navigation
        // path using dynamic LINQ (System.Linq.Dynamic.Core), same pattern as
        // ApplicationRepository.MapSortingField for the main Applications list.
        var coreField = OnboardingCoreFieldRegistry.Fields
            .FirstOrDefault(f => f.Key.Equals(field, StringComparison.OrdinalIgnoreCase));
        if (coreField != null)
        {
            try
            {
                return query.OrderBy($"{coreField.EfPath} {(desc ? "DESC" : "ASC")}");
            }
            catch
            {
                // Unsupported dynamic LINQ translation — fall through to the default sort.
            }
        }

        return query.OrderByDescending(a => a.SubmissionDate);
    }

    private static IQueryable<Application> ApplyCoreFieldFilter(IQueryable<Application> query, string fieldName, string loweredValue)
    {
        var coreField = OnboardingCoreFieldRegistry.Fields
            .FirstOrDefault(f => f.IsTextFilterable && f.Key.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        if (coreField == null) return query;

        try
        {
            return query.Where($"{coreField.EfPath} != null && {coreField.EfPath}.ToLower().Contains(@0)", loweredValue);
        }
        catch
        {
            // Unsupported dynamic LINQ translation — skip this filter rather than fail the whole request.
            return query;
        }
    }

    private async Task<List<Guid>?> MergeCoreFieldGlobalMatchesAsync(
        string category, string filter, IReadOnlyList<Guid>? existingMatchIds)
    {
        var coreMatches = await GetCoreFieldGlobalMatchIdsAsync(category, filter);
        if (coreMatches.Count == 0) return existingMatchIds?.ToList();

        var merged = new HashSet<Guid>(coreMatches);
        if (existingMatchIds != null) merged.UnionWith(existingMatchIds);
        return merged.ToList();
    }

    private async Task<List<Guid>> GetCoreFieldGlobalMatchIdsAsync(string category, string filter)
    {
        var textFields = (await GetMappedCoreFieldsAsync(category)).Where(f => f.IsTextFilterable).ToList();
        if (textFields.Count == 0) return [];

        var loweredFilter = filter.ToLowerInvariant();
        var predicate = string.Join(" || ", textFields.Select(f => $"({f.EfPath} != null && {f.EfPath}.ToLower().Contains(@0))"));

        try
        {
            var query = await BuildQueryAsync(category);
            return await query.Where(predicate, loweredFilter).Select(a => a.Id).ToListAsync();
        }
        catch
        {
            // Unsupported dynamic LINQ translation for one of the mapped fields — skip core-field matching.
            return [];
        }
    }
}
