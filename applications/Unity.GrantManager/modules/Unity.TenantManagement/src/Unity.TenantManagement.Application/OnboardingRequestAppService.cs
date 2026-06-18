#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;
using Unity.Flex.WorksheetInstances;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement.Onboarding;
using Unity.TenantManagement.Validation;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.SettingManagement;

namespace Unity.TenantManagement;

[Authorize(IdentityConsts.ITOperationsPolicyName)]
public class OnboardingRequestAppService(
    IEnumerable<IOnboardingValidationStep> validationSteps,
    ISettingManager settingManager
) : ApplicationService, IOnboardingRequestAppService
{
    private readonly IEnumerable<IOnboardingValidationStep> _validationSteps = validationSteps;
    private readonly ISettingManager _settingManager = settingManager;

    private const string DefaultCategory = "Onboarding";
    private const string ApplicationCorrelationProvider = "Application";
    private const string UserProvider = "U";

    private ITenantAppService TenantAppService =>
        LazyServiceProvider.LazyGetRequiredService<ITenantAppService>();

    private IOnboardingUserLookup? UserLookup =>
        LazyServiceProvider.LazyGetService<IOnboardingUserLookup>();

    private IOnboardingApplicationProvider? ApplicationProvider =>
        LazyServiceProvider.LazyGetService<IOnboardingApplicationProvider>();

    private IWorksheetAppService WorksheetAppService =>
        LazyServiceProvider.LazyGetRequiredService<IWorksheetAppService>();

    private IWorksheetInstanceAppService WorksheetInstanceAppService =>
        LazyServiceProvider.LazyGetRequiredService<IWorksheetInstanceAppService>();

    public virtual async Task<PagedResultDto<OnboardingRequestDto>> GetListAsync(OnboardingListRequestDto input)
    {
        if (ApplicationProvider == null)
            return new PagedResultDto<OnboardingRequestDto>(0, []);

        var category = string.IsNullOrWhiteSpace(input.Category) ? DefaultCategory : input.Category;

        bool hasGlobalFilter = !string.IsNullOrWhiteSpace(input.Filter);
        var staticColumnFilters = GetStaticColumnFilters(input.ColumnFilters);
        var dynamicColumnFilters = GetDynamicColumnFilters(input.ColumnFilters);
        var (sortField, sortDescending) = ParseSorting(input.Sorting);
        bool isDynamicSort = sortField != null && !IsStaticColumn(sortField);

        IReadOnlyList<Guid>? globalDynamicMatchIds = null;
        IReadOnlyList<Guid>? dynamicColumnMatchIds = null;
        Dictionary<Guid, IReadOnlyList<WorksheetInstanceDataDto>>? allInstancesByApp = null;

        if (hasGlobalFilter || dynamicColumnFilters.Count > 0 || isDynamicSort)
        {
            var allIds = await ApplicationProvider.GetAllIdsAsync(category);

            if (allIds.Count > 0)
            {
                var allInstances = await WorksheetInstanceAppService
                    .GetListByCorrelationIdsAsync(allIds, ApplicationCorrelationProvider);

                allInstancesByApp = allInstances
                    .GroupBy(wi => wi.CorrelationId)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<WorksheetInstanceDataDto>)g.ToList());

                if (hasGlobalFilter)
                {
                    globalDynamicMatchIds = allIds
                        .Where(id => MatchesGlobalInWorksheet(
                            allInstancesByApp.GetValueOrDefault(id, []), input.Filter!))
                        .ToList();
                }

                if (dynamicColumnFilters.Count > 0)
                {
                    dynamicColumnMatchIds = allIds
                        .Where(id => dynamicColumnFilters.All(cf =>
                            MatchesColumnInWorksheet(
                                allInstancesByApp.GetValueOrDefault(id, []), cf.Name, cf.Value)))
                        .ToList();
                }
            }
            else if (dynamicColumnFilters.Count > 0)
            {
                dynamicColumnMatchIds = []; // No apps → nothing matches dynamic filters
            }
        }

        PagedResultDto<OnboardingApplicationRecord> appPage;

        if (isDynamicSort)
        {
            // Worksheet field values live in JSONB, not a queryable column, so the DB can't
            // sort by them — fetch every filtered match and sort/page in memory instead.
            var filtered = await ApplicationProvider.GetPagedListAsync(
                0, int.MaxValue, null, category,
                hasGlobalFilter ? input.Filter : null,
                globalDynamicMatchIds,
                staticColumnFilters.Count > 0 ? staticColumnFilters : null,
                dynamicColumnMatchIds);

            IOrderedEnumerable<OnboardingApplicationRecord> ordered = sortDescending
                ? filtered.Items.OrderByDescending(
                    a => GetWorksheetFieldValue(allInstancesByApp?.GetValueOrDefault(a.Id, []) ?? [], sortField!),
                    StringComparer.OrdinalIgnoreCase)
                : filtered.Items.OrderBy(
                    a => GetWorksheetFieldValue(allInstancesByApp?.GetValueOrDefault(a.Id, []) ?? [], sortField!),
                    StringComparer.OrdinalIgnoreCase);

            var pagedItems = ordered.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
            appPage = new PagedResultDto<OnboardingApplicationRecord>(filtered.TotalCount, pagedItems);
        }
        else
        {
            appPage = await ApplicationProvider.GetPagedListAsync(
                input.SkipCount, input.MaxResultCount, input.Sorting, category,
                hasGlobalFilter ? input.Filter : null,
                globalDynamicMatchIds,
                staticColumnFilters.Count > 0 ? staticColumnFilters : null,
                dynamicColumnMatchIds);
        }

        if (appPage.TotalCount == 0)
            return new PagedResultDto<OnboardingRequestDto>(0, []);

        var applicationIds = appPage.Items.Select(a => a.Id).ToList();
        var worksheetInstances = await WorksheetInstanceAppService
            .GetListByCorrelationIdsAsync(applicationIds, ApplicationCorrelationProvider);

        var instancesByApp = worksheetInstances
            .GroupBy(wi => wi.CorrelationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var mapping = await ReadTenantMappingAsync();
        var items = appPage.Items
            .Select(app => MapToDto(
                app,
                instancesByApp.GetValueOrDefault(app.Id, []),
                mapping))
            .ToList();

        return new PagedResultDto<OnboardingRequestDto>(appPage.TotalCount, items);
    }

    public virtual async Task<OnboardingRequestDto?> GetAsync(Guid id)
    {
        if (ApplicationProvider == null) return null;

        var app = await ApplicationProvider.GetByIdAsync(id);
        if (app == null) return null;

        var worksheetInstances = await WorksheetInstanceAppService
            .GetListByCorrelationIdsAsync([id], ApplicationCorrelationProvider);

        var mapping = await ReadTenantMappingAsync();
        return MapToDto(app, worksheetInstances, mapping);
    }

    public virtual async Task<OnboardingColumnSchemaDto> GetColumnSchemaAsync(string? category = null)
    {
        if (ApplicationProvider == null)
            return new OnboardingColumnSchemaDto { Columns = [] };

        var resolvedCategory = string.IsNullOrWhiteSpace(category) ? DefaultCategory : category;
        var onboardingAppIds = await ApplicationProvider.GetAllIdsAsync(resolvedCategory);

        if (onboardingAppIds.Count == 0)
            return new OnboardingColumnSchemaDto { Columns = [] };

        var worksheetIds = await WorksheetInstanceAppService
            .GetDistinctWorksheetIdsByCorrelationIdsAsync(onboardingAppIds, ApplicationCorrelationProvider);

        var seenKeys = new HashSet<string>();
        var columns = new List<OnboardingColumnDto>();

        foreach (var worksheetId in worksheetIds)
        {
            WorksheetDto worksheet;
            try { worksheet = await WorksheetAppService.GetAsync(worksheetId); }
            catch { continue; }

            foreach (var field in worksheet.Sections
                .OrderBy(s => s.Order)
                .SelectMany(s => s.Fields.OrderBy(f => f.Order))
                .Where(f => f.Enabled))
            {
                if (!seenKeys.Add($"{field.Key}|{field.Label}")) continue;
                columns.Add(new OnboardingColumnDto
                {
                    Key = field.Key,
                    Label = field.Label,
                    Type = field.Type.ToString(),
                    Selected = true
                });
            }
        }

        var saved = await ReadTenantMappingAsync();
        saved.Columns = columns;
        return saved;
    }

    public virtual async Task<List<string>> GetAvailableCategoriesAsync()
    {
        if (ApplicationProvider == null) return [DefaultCategory];
        var categories = await ApplicationProvider.GetAvailableCategoriesAsync();
        // Always ensure "Onboarding" is present even if no forms carry it yet
        if (!categories.Contains(DefaultCategory))
            categories.Insert(0, DefaultCategory);
        return categories;
    }

    public virtual async Task<OnboardingValidationResultDto> ValidateAsync(Guid id, string? tenantNameFieldKey, string? superUsersFieldKey, string? branchFieldKey = null, string? featuresFieldKey = null, string? ministryFieldKey = null, string? programAreaFieldKey = null)
    {
        var request = await GetAsync(id);
        if (request == null)
            return new OnboardingValidationResultDto { IsValid = false, Issues = ["Onboarding request not found."] };

        await ResolveFieldMappings(request, tenantNameFieldKey, superUsersFieldKey, branchFieldKey, featuresFieldKey, ministryFieldKey, programAreaFieldKey);

        var issues = new List<string>();
        foreach (var step in _validationSteps.OrderBy(s => s.Order))
        {
            var stepResult = await step.ValidateAsync(request);
            if (!stepResult.IsValid && stepResult.Issue is not null)
                issues.Add($"[{step.StepName}] {stepResult.Issue}");
        }

        return new OnboardingValidationResultDto { IsValid = issues.Count == 0, Issues = issues };
    }

    public virtual async Task CreateTenantAsync(Guid id, CreateTenantInputDto? input)
    {
        var request = await GetAsync(id)
            ?? throw new UserFriendlyException("Onboarding request not found.");

        await ResolveFieldMappings(request, input?.TenantNameFieldKey, input?.SuperUsersFieldKey, input?.BranchFieldKey, input?.FeaturesFieldKey, input?.MinistryFieldKey, input?.ProgramAreaFieldKey);

        if (input != null)
            await SaveFieldMappingAsync(input.TenantNameFieldKey, input.SuperUsersFieldKey, input.BranchFieldKey, input.FeaturesFieldKey, input.MinistryFieldKey, input.ProgramAreaFieldKey);

        var emails = SuperUsersValidationStep.ParseEmails(request.SuperUsers);

        var userGuids = new List<string>();
        if (UserLookup is not null)
        {
            foreach (var email in emails)
            {
                var guid = await UserLookup.FindUserGuidByEmailAsync(email);
                if (!string.IsNullOrWhiteSpace(guid))
                    userGuids.Add(guid);
            }
        }

        if (userGuids.Count == 0)
            throw new UserFriendlyException("No valid super users could be resolved. Cannot create tenant without at least one valid program manager.");

        var featureKeys = OnboardingFeatureMap.ResolveFeatureKeys(request.Features);

        var tenantDto = await TenantAppService.CreateAsync(new TenantCreateDto
        {
            Name = request.TenantName,
            Branch = request.Branch,
            Description = request.TenantDescription,
            UserIdentifier = userGuids[0],
            FeatureKeys = featureKeys.Count > 0 ? string.Join(',', featureKeys) : null
        });

        foreach (var userGuid in userGuids.Skip(1))
        {
            await TenantAppService.AssignManagerAsync(new TenantAssignManagerDto
            {
                TenantId = tenantDto.Id,
                UserIdentifier = userGuid
            });
        }

        if (ApplicationProvider != null)
            await ApplicationProvider.CloseApplicationAsync(id);
    }

    private async Task ResolveFieldMappings(OnboardingRequestDto request,
        string? tenantNameKey = null, string? superUsersKey = null,
        string? branchKey = null, string? featuresKey = null,
        string? ministryKey = null, string? programAreaKey = null)
    {
        var saved = await ReadTenantMappingAsync();
        tenantNameKey ??= saved.TenantNameFieldKey;
        superUsersKey ??= saved.SuperUsersFieldKey;
        branchKey ??= saved.BranchFieldKey;
        featuresKey ??= saved.FeaturesFieldKey;
        ministryKey ??= saved.MinistryFieldKey;
        programAreaKey ??= saved.ProgramAreaFieldKey;

        if (!string.IsNullOrEmpty(tenantNameKey) && request.Fields.TryGetValue(tenantNameKey, out var tenantNameVal) && tenantNameVal is not null)
            request.TenantName = tenantNameVal.ToString()!;
        if (!string.IsNullOrEmpty(superUsersKey) && request.Fields.TryGetValue(superUsersKey, out var superUsersVal) && superUsersVal is not null)
            request.SuperUsers = superUsersVal.ToString()!;
        if (!string.IsNullOrEmpty(branchKey) && request.Fields.TryGetValue(branchKey, out var branchVal) && branchVal is not null)
            request.Branch = branchVal.ToString()!;
        if (!string.IsNullOrEmpty(featuresKey) && request.Fields.TryGetValue(featuresKey, out var featuresVal) && featuresVal is not null)
            request.Features = featuresVal.ToString()!;
        if (!string.IsNullOrEmpty(ministryKey) && request.Fields.TryGetValue(ministryKey, out var ministryVal) && ministryVal is not null)
            request.Ministry = ministryVal.ToString()!;
        if (!string.IsNullOrEmpty(programAreaKey) && request.Fields.TryGetValue(programAreaKey, out var programAreaVal) && programAreaVal is not null)
            request.ProgramAreaName = programAreaVal.ToString()!;
    }

    private async Task SaveFieldMappingAsync(string? tenantNameKey, string? superUsersKey, string? branchKey, string? featuresKey, string? ministryKey, string? programAreaKey)
    {
        var userId = CurrentUser.Id?.ToString();
        if (string.IsNullOrEmpty(userId)) return;
        await _settingManager.SetAsync(OnboardingColumnConfigSettings.TenantNameFieldKey, tenantNameKey, UserProvider, userId);
        await _settingManager.SetAsync(OnboardingColumnConfigSettings.SuperUsersFieldKey, superUsersKey, UserProvider, userId);
        await _settingManager.SetAsync(OnboardingColumnConfigSettings.BranchFieldKey, branchKey, UserProvider, userId);
        await _settingManager.SetAsync(OnboardingColumnConfigSettings.FeaturesFieldKey, featuresKey, UserProvider, userId);
        await _settingManager.SetAsync(OnboardingColumnConfigSettings.MinistryFieldKey, ministryKey, UserProvider, userId);
        await _settingManager.SetAsync(OnboardingColumnConfigSettings.ProgramAreaFieldKey, programAreaKey, UserProvider, userId);
    }

    private async Task<OnboardingColumnSchemaDto> ReadTenantMappingAsync()
    {
        var userId = CurrentUser.Id?.ToString();
        string? tenantNameKey = null, superUsersKey = null, branchKey = null, featuresKey = null, ministryKey = null, programAreaKey = null;

        if (!string.IsNullOrEmpty(userId))
        {
            tenantNameKey  = await _settingManager.GetOrNullAsync(OnboardingColumnConfigSettings.TenantNameFieldKey,  UserProvider, userId);
            superUsersKey  = await _settingManager.GetOrNullAsync(OnboardingColumnConfigSettings.SuperUsersFieldKey,  UserProvider, userId);
            branchKey      = await _settingManager.GetOrNullAsync(OnboardingColumnConfigSettings.BranchFieldKey,      UserProvider, userId);
            featuresKey    = await _settingManager.GetOrNullAsync(OnboardingColumnConfigSettings.FeaturesFieldKey,    UserProvider, userId);
            ministryKey    = await _settingManager.GetOrNullAsync(OnboardingColumnConfigSettings.MinistryFieldKey,    UserProvider, userId);
            programAreaKey = await _settingManager.GetOrNullAsync(OnboardingColumnConfigSettings.ProgramAreaFieldKey, UserProvider, userId);
        }

        tenantNameKey  ??= await _settingManager.GetOrNullGlobalAsync(OnboardingColumnConfigSettings.TenantNameFieldKey);
        superUsersKey  ??= await _settingManager.GetOrNullGlobalAsync(OnboardingColumnConfigSettings.SuperUsersFieldKey);
        branchKey      ??= await _settingManager.GetOrNullGlobalAsync(OnboardingColumnConfigSettings.BranchFieldKey);
        featuresKey    ??= await _settingManager.GetOrNullGlobalAsync(OnboardingColumnConfigSettings.FeaturesFieldKey);
        ministryKey    ??= await _settingManager.GetOrNullGlobalAsync(OnboardingColumnConfigSettings.MinistryFieldKey);
        programAreaKey ??= await _settingManager.GetOrNullGlobalAsync(OnboardingColumnConfigSettings.ProgramAreaFieldKey);

        return new OnboardingColumnSchemaDto
        {
            TenantNameFieldKey  = tenantNameKey,
            SuperUsersFieldKey  = superUsersKey,
            BranchFieldKey      = branchKey,
            FeaturesFieldKey    = featuresKey,
            MinistryFieldKey    = ministryKey,
            ProgramAreaFieldKey = programAreaKey
        };
    }

    private static List<ColumnFilterDto> GetStaticColumnFilters(List<ColumnFilterDto>? filters) =>
        (filters ?? [])
            .Where(cf => !string.IsNullOrWhiteSpace(cf.Value) && IsStaticColumn(cf.Name))
            .ToList();

    private static List<ColumnFilterDto> GetDynamicColumnFilters(List<ColumnFilterDto>? filters) =>
        (filters ?? [])
            .Where(cf => !string.IsNullOrWhiteSpace(cf.Value) && !IsStaticColumn(cf.Name))
            .ToList();

    private static bool IsStaticColumn(string name) =>
        name is "submissionNumber" or "status" or "category" or "submissionDate";

    private static (string? Field, bool Descending) ParseSorting(string? sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting)) return (null, false);
        var parts = sorting.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var descending = parts.Length > 1 && parts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase);

        // Dynamic columns use a "fields.<key>" dot-path on the client so DataTables can
        // resolve the cell value; strip that prefix to get back the raw worksheet field key.
        var field = parts[0];
        if (field.StartsWith("fields.", StringComparison.OrdinalIgnoreCase))
            field = field["fields.".Length..];

        return (field, descending);
    }

    private static string? GetWorksheetFieldValue(
        IReadOnlyList<WorksheetInstanceDataDto> instances, string fieldKey)
    {
        foreach (var wi in instances)
        {
            if (string.IsNullOrWhiteSpace(wi.CurrentValue) || wi.CurrentValue == "{}") continue;
            try
            {
                var parsed = JsonSerializer.Deserialize<WorksheetInstanceValue>(wi.CurrentValue);
                var match = parsed?.Values?.FirstOrDefault(fv =>
                    fv.Key.Equals(fieldKey, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match.Value;
            }
            catch
            {
                // Malformed JSONB — skip silently
            }
        }
        return null;
    }

    private static bool MatchesGlobalInWorksheet(
        IReadOnlyList<WorksheetInstanceDataDto> instances, string filter)
    {
        var lowerFilter = filter.ToLowerInvariant();
        return instances.Any(wi =>
        {
            if (string.IsNullOrWhiteSpace(wi.CurrentValue) || wi.CurrentValue == "{}") return false;
            try
            {
                var parsed = JsonSerializer.Deserialize<WorksheetInstanceValue>(wi.CurrentValue);
                return parsed?.Values?.Any(fv =>
                    fv.Value?.ToLowerInvariant().Contains(lowerFilter) ?? false) ?? false;
            }
            catch { return false; }
        });
    }

    private static bool MatchesColumnInWorksheet(
        IReadOnlyList<WorksheetInstanceDataDto> instances, string fieldKey, string value)
    {
        var lowerValue = value.ToLowerInvariant();
        return instances.Any(wi =>
        {
            if (string.IsNullOrWhiteSpace(wi.CurrentValue) || wi.CurrentValue == "{}") return false;
            try
            {
                var parsed = JsonSerializer.Deserialize<WorksheetInstanceValue>(wi.CurrentValue);
                return parsed?.Values?.Any(fv =>
                    fv.Key.Equals(fieldKey, StringComparison.OrdinalIgnoreCase) &&
                    (fv.Value?.ToLowerInvariant().Contains(lowerValue) ?? false)) ?? false;
            }
            catch { return false; }
        });
    }

    private static OnboardingRequestDto MapToDto(
        OnboardingApplicationRecord app,
        IEnumerable<WorksheetInstanceDataDto> worksheetInstances,
        OnboardingColumnSchemaDto mapping)
    {
        var dto = new OnboardingRequestDto
        {
            Id = app.Id,
            SubmissionNumber = app.ReferenceNo,
            SubmissionDate = app.SubmissionDate,
            Status = app.Status,
            Category = app.Category
        };

        foreach (var instance in worksheetInstances)
        {
            if (string.IsNullOrWhiteSpace(instance.CurrentValue) || instance.CurrentValue == "{}") continue;

            try
            {
                var parsed = JsonSerializer.Deserialize<WorksheetInstanceValue>(instance.CurrentValue);
                if (parsed?.Values == null) continue;

                foreach (var fv in parsed.Values)
                {
                    dto.Fields[fv.Key] = fv.Value;

                    switch (fv.Key.ToLowerInvariant().Replace("-", "").Replace("_", "").Replace(" ", ""))
                    {
                        case "tenantdescription": case "description": dto.TenantDescription = fv.Value; break;
                        case "programareaname": case "programarea": dto.ProgramAreaName = fv.Value; break;
                        case "programareadescription": dto.ProgramAreaDescription = fv.Value; break;
                        case "contacts": dto.Contacts = fv.Value; break;
                        case "features": dto.Features = fv.Value; break;
                        case "executivedirector": dto.ExecutiveDirector = fv.Value; break;
                        case "branch": dto.Branch = fv.Value; break;
                        case "ministry": dto.Ministry = fv.Value; break;
                    }
                }
            }
            catch
            {
                // Malformed JSONB — skip silently
            }
        }

        if (!string.IsNullOrEmpty(mapping.TenantNameFieldKey) && dto.Fields.TryGetValue(mapping.TenantNameFieldKey, out var tn) && tn != null)
            dto.TenantName = tn.ToString()!;
        if (!string.IsNullOrEmpty(mapping.SuperUsersFieldKey) && dto.Fields.TryGetValue(mapping.SuperUsersFieldKey, out var su) && su != null)
            dto.SuperUsers = su.ToString()!;
        if (!string.IsNullOrEmpty(mapping.BranchFieldKey) && dto.Fields.TryGetValue(mapping.BranchFieldKey, out var br) && br != null)
            dto.Branch = br.ToString()!;
        if (!string.IsNullOrEmpty(mapping.FeaturesFieldKey) && dto.Fields.TryGetValue(mapping.FeaturesFieldKey, out var ft) && ft != null)
            dto.Features = ft.ToString()!;
        if (!string.IsNullOrEmpty(mapping.MinistryFieldKey) && dto.Fields.TryGetValue(mapping.MinistryFieldKey, out var mn) && mn != null)
            dto.Ministry = mn.ToString()!;
        if (!string.IsNullOrEmpty(mapping.ProgramAreaFieldKey) && dto.Fields.TryGetValue(mapping.ProgramAreaFieldKey, out var pa) && pa != null)
            dto.ProgramAreaName = pa.ToString()!;

        return dto;
    }
}
