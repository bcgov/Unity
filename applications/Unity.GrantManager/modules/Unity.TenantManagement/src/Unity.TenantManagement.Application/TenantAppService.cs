using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.TenantManagement.Abstractions;
using Unity.TenantManagement.Application;
using Unity.TenantManagement.Application.Contracts;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;
using Volo.Abp.DependencyInjection;

namespace Unity.TenantManagement;

[Authorize(TenantManagementPermissions.Tenants.Default)]
[ExposeServices(typeof(ITenantAppService), typeof(TenantAppService))]
public class TenantAppService(
     ICurrentTenant currentTenant,
    ITenantRepository tenantRepository,
    ITenantManager tenantManager,
    ILocalEventBus localEventBus,
    IUnitOfWorkManager unitOfWorkManager,
    ITenantConnectionStringBuilder tenantConnectionStringBuilder) : TenantManagementAppServiceBase, ITenantAppService
{
    private const string ExtraPropDivision = "Division";
    private const string ExtraPropBranch = "Branch";
    private const string ExtraPropDescription = "Description";
    private const string ExtraPropCasClientCode = "CasClientCode";

    public virtual async Task<TenantDto> GetAsync(Guid id)
    {
        return ObjectMapper.Map<Tenant, TenantDto>(
            await tenantRepository.GetAsync(id)
        );
    }

    public virtual async Task<PagedResultDto<TenantDto>> GetListAsync(GetTenantsInput input)
    {
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            input.Sorting = nameof(Tenant.Name);
        }

        var sortParts = input.Sorting.Trim().Split(' ');
        var sortField = sortParts[0].Trim();
        var sortDescending = sortParts.Length > 1 && sortParts[1].Trim().Equals("DESC", StringComparison.OrdinalIgnoreCase);

        var extraPropertySortFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ExtraPropDivision, ExtraPropBranch, ExtraPropDescription, ExtraPropCasClientCode
        };

        var dbSortFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(Tenant.Id),
            nameof(Tenant.Name),
            nameof(Tenant.NormalizedName),
            nameof(Tenant.CreationTime),
            nameof(Tenant.LastModificationTime)
        };

        bool sortInMemory = extraPropertySortFields.Contains(sortField);
        bool hasFilter = !input.Filter.IsNullOrWhiteSpace();

        // Use fast DB path when sorting and filtering on native fields only
        if (!sortInMemory && !hasFilter)
        {
            var count = await tenantRepository.GetCountAsync(input.Filter);
            var list = await tenantRepository.GetListAsync(
                input.Sorting,
                input.MaxResultCount,
                input.SkipCount,
                input.Filter
            );
            return new PagedResultDto<TenantDto>(
                count,
                ObjectMapper.Map<List<Tenant>, List<TenantDto>>(list)
            );
        }

        // In-memory path: needed when filtering on ExtraProperties or sorting on ExtraProperties
        var dbSorting = dbSortFields.Contains(sortField) ? input.Sorting : nameof(Tenant.Name);
        var allTenants = await tenantRepository.GetListAsync(dbSorting, int.MaxValue, 0, null);

        IEnumerable<Tenant> result = allTenants;

        // Apply ExtraProperties filter
        if (hasFilter)
        {
            var filter = input.Filter.Trim();
            result = result.Where(t =>
                (t.Name != null && t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                MatchesExtraProperty(t, ExtraPropDivision, filter) ||
                MatchesExtraProperty(t, ExtraPropBranch, filter) ||
                MatchesExtraProperty(t, ExtraPropDescription, filter) ||
                MatchesExtraProperty(t, ExtraPropCasClientCode, filter));
        }

        // Apply ExtraProperties sort
        if (sortInMemory)
        {
            result = sortDescending
                ? result.OrderByDescending(t => GetExtraPropertyValue(t, sortField))
                : result.OrderBy(t => GetExtraPropertyValue(t, sortField));
        }

        var resultList = result.ToList();
        var paged = resultList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        return new PagedResultDto<TenantDto>(
            resultList.Count,
            ObjectMapper.Map<List<Tenant>, List<TenantDto>>(paged)
        );
    }

    private static bool MatchesExtraProperty(Tenant tenant, string key, string filter)
    {
        return tenant.ExtraProperties.TryGetValue(key, out var value)
            && value?.ToString()?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string GetExtraPropertyValue(Tenant tenant, string key)
    {
        var entry = tenant.ExtraProperties
            .FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return entry.Value?.ToString() ?? string.Empty;
    }

    [Authorize(TenantManagementPermissions.Tenants.Create)]
    public virtual async Task<TenantDto> CreateAsync(TenantCreateDto input)
    {
        Tenant tenant = null;

        using (var uow = unitOfWorkManager.Begin(true, false))
        {
            tenant = await tenantManager.CreateAsync(input.Name);

            var credentials = await tenantConnectionStringBuilder.GenerateCredentialsAsync();

            tenant.ConnectionStrings
                .Add(new TenantConnectionString(tenant.Id,
                    UnityTenantManagementConsts.TenantConnectionStringName,
                    tenantConnectionStringBuilder.Build(tenant.Name, credentials)));

            // Set ExtraProperties from input
            tenant.ExtraProperties[UnityTenantManagementConsts.TenantLicencePlateExtraPropertyKey] = credentials.DbName;
            tenant.ExtraProperties[ExtraPropDivision] = input.Division ?? string.Empty;
            tenant.ExtraProperties[ExtraPropBranch] = input.Branch ?? string.Empty;
            tenant.ExtraProperties[ExtraPropDescription] = input.Description ?? string.Empty;
            tenant.ExtraProperties[ExtraPropCasClientCode] = input.CasClientCode ?? string.Empty;

            await tenantRepository.InsertAsync(tenant);

            await uow.SaveChangesAsync();
            await uow.CompleteAsync();
        }

        await localEventBus.PublishAsync(
                new TenantCreatedEto
                {
                    Id = tenant.Id,
                    Name = tenant.Name,
                    Properties =
                    {
                        { "UserIdentifier",  input.UserIdentifier }
                    }
                }
            );

        return ObjectMapper.Map<Tenant, TenantDto>(tenant);
    }

    [Authorize(TenantManagementPermissions.Tenants.Update)]
    public virtual async Task<TenantDto> UpdateAsync(Guid id, TenantUpdateDto input)
    {
        var tenant = await tenantRepository.GetAsync(id);

        await tenantManager.ChangeNameAsync(tenant, input.Name);

        tenant.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        
        // Update ExtraProperties from input
        tenant.ExtraProperties[ExtraPropDivision] = input.Division ?? string.Empty;
        tenant.ExtraProperties[ExtraPropBranch] = input.Branch ?? string.Empty;
        tenant.ExtraProperties[ExtraPropDescription] = input.Description ?? string.Empty;
        tenant.ExtraProperties[ExtraPropCasClientCode] = input.CasClientCode ?? string.Empty;

        await tenantRepository.UpdateAsync(tenant);

        return ObjectMapper.Map<Tenant, TenantDto>(tenant);
    }

    [Authorize(TenantManagementPermissions.Tenants.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var tenant = await tenantRepository.FindAsync(id);
        if (tenant == null)
        {
            return;
        }

        await tenantRepository.DeleteAsync(tenant);
    }

    [Authorize(TenantManagementPermissions.Tenants.ManageConnectionStrings)]
    public virtual async Task<string> GetDefaultConnectionStringAsync(Guid id)
    {
        var tenant = await tenantRepository.GetAsync(id);
        return tenant?.FindDefaultConnectionString();
    }

    [Authorize(TenantManagementPermissions.Tenants.ManageConnectionStrings)]
    public virtual async Task UpdateDefaultConnectionStringAsync(Guid id, string defaultConnectionString)
    {
        var tenant = await tenantRepository.GetAsync(id);
        tenant.SetDefaultConnectionString(defaultConnectionString);
        await tenantRepository.UpdateAsync(tenant);
    }

    [Authorize(TenantManagementPermissions.Tenants.ManageConnectionStrings)]
    public virtual async Task DeleteDefaultConnectionStringAsync(Guid id)
    {
        var tenant = await tenantRepository.GetAsync(id);
        tenant.RemoveDefaultConnectionString();
        await tenantRepository.UpdateAsync(tenant);
    }

    [RemoteService(false)]    
    [AllowAnonymous]
    public async Task<string> GetCurrentTenantCasClientCodeAsync(Guid tenantId)
    {
        var tenant = tenantId != Guid.Empty ? await tenantRepository.GetAsync(tenantId) : null;
        return tenant?.ExtraProperties.TryGetValue("CasClientCode", out var value) == true ? value?.ToString() : null;
    }

    public async Task<string> GetCurrentTenantName()
    {
        var tenantId = currentTenant.GetId();
        var tenant = tenantId != Guid.Empty ? await tenantRepository.GetAsync(tenantId) : null;        
        return tenant?.Name ?? string.Empty;
    }

    public async Task AssignManagerAsync(TenantAssignManagerDto managerAssignment)
    {
        await localEventBus.PublishAsync(
             new TenantAssignManagerEto
             {
                 TenantId = managerAssignment.TenantId,
                 UserIdentifier = managerAssignment.UserIdentifier
             }
         );
    }
}
