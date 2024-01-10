using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Unity.TenantManagement.Application;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.EventBus.Local;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectExtending;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;

namespace Unity.TenantManagement;

[Authorize(TenantManagementPermissions.Tenants.Default)]
public class TenantAppService : TenantManagementAppServiceBase, ITenantAppService
{
    protected IDataSeeder DataSeeder { get; }
    protected ITenantRepository TenantRepository { get; }
    protected ITenantManager TenantManager { get; }

    private readonly ILocalEventBus _localEventBus;

    private readonly IUnitOfWorkManager _unitOfWorkManager;

    private readonly IConfiguration _configuration;

    public TenantAppService(
        ITenantRepository tenantRepository,
        ITenantManager tenantManager,
        IDataSeeder dataSeeder,
        ILocalEventBus localEventBus,
        IUnitOfWorkManager unitOfWorkManager,
        IConfiguration configuration)
    {
        DataSeeder = dataSeeder;
        TenantRepository = tenantRepository;
        TenantManager = tenantManager;
        _localEventBus = localEventBus;
        _unitOfWorkManager = unitOfWorkManager;
        _configuration = configuration;
    }

    public virtual async Task<TenantDto> GetAsync(Guid id)
    {
        return ObjectMapper.Map<Tenant, TenantDto>(
            await TenantRepository.GetAsync(id)
        );
    }

    public virtual async Task<PagedResultDto<TenantDto>> GetListAsync(GetTenantsInput input)
    {
        if (input.Sorting.IsNullOrWhiteSpace())
        {
            input.Sorting = nameof(Tenant.Name);
        }

        var count = await TenantRepository.GetCountAsync(input.Filter);
        var list = await TenantRepository.GetListAsync(
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

    [Authorize(TenantManagementPermissions.Tenants.Create)]
    public virtual async Task<TenantDto> CreateAsync(TenantCreateDto input)
    {
        Tenant tenant = null;

        using (var uow = _unitOfWorkManager.Begin(true, false))
        {
            tenant = await TenantManager.CreateAsync(input.Name);

            tenant.ConnectionStrings
                .Add(new TenantConnectionString(tenant.Id,
                    UnityTenantManagementConsts.TenantConnectionStringName,
                    BuildTenantConnectionString(tenant.Name)));

            input.MapExtraPropertiesTo(tenant);

            await TenantRepository.InsertAsync(tenant);

            await uow.SaveChangesAsync();
            await uow.CompleteAsync();
        }

        await _localEventBus.PublishAsync(
                new TenantCreatedEto
                {
                    Id = tenant.Id,
                    Name = tenant.Name,
                    Properties =
                    {
                        { "AdminIdentifier",  input.AdminIdentifier }
                    }
                }
            );

        return ObjectMapper.Map<Tenant, TenantDto>(tenant);
    }

    private string BuildTenantConnectionString(string tenantName)
    {
        var connectionString = _configuration.GetConnectionString(UnityTenantManagementConsts.TenantConnectionStringName);

        return connectionString == null
            ? throw new UserFriendlyException("Connection string configuration error")
            : connectionString
            .Replace(UnityTenantManagementConsts.TenantConnectionStringTenantDb, PrepTenantName(tenantName));
    }
    private static string PrepTenantName(string tenantName)
    {
        return tenantName.Trim().Replace(" ", "");
    }

    [Authorize(TenantManagementPermissions.Tenants.Update)]
    public virtual async Task<TenantDto> UpdateAsync(Guid id, TenantUpdateDto input)
    {
        var tenant = await TenantRepository.GetAsync(id);

        await TenantManager.ChangeNameAsync(tenant, input.Name);

        tenant.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        input.MapExtraPropertiesTo(tenant);

        await TenantRepository.UpdateAsync(tenant);

        return ObjectMapper.Map<Tenant, TenantDto>(tenant);
    }

    [Authorize(TenantManagementPermissions.Tenants.Delete)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var tenant = await TenantRepository.FindAsync(id);
        if (tenant == null)
        {
            return;
        }

        await TenantRepository.DeleteAsync(tenant);
    }

    [Authorize(TenantManagementPermissions.Tenants.ManageConnectionStrings)]
    public virtual async Task<string> GetDefaultConnectionStringAsync(Guid id)
    {
        var tenant = await TenantRepository.GetAsync(id);
        return tenant?.FindDefaultConnectionString();
    }

    [Authorize(TenantManagementPermissions.Tenants.ManageConnectionStrings)]
    public virtual async Task UpdateDefaultConnectionStringAsync(Guid id, string defaultConnectionString)
    {
        var tenant = await TenantRepository.GetAsync(id);
        tenant.SetDefaultConnectionString(defaultConnectionString);
        await TenantRepository.UpdateAsync(tenant);
    }

    [Authorize(TenantManagementPermissions.Tenants.ManageConnectionStrings)]
    public virtual async Task DeleteDefaultConnectionStringAsync(Guid id)
    {
        var tenant = await TenantRepository.GetAsync(id);
        tenant.RemoveDefaultConnectionString();
        await TenantRepository.UpdateAsync(tenant);
    }
}
