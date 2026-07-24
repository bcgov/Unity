using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.SettingManagement;

[Authorize(UnitySettingManagementPermissions.EditProgramDetails)]
public class ProgramDetailsAppService(ITenantRepository tenantRepository)
    : GrantManagerAppService, IProgramDetailsAppService
{
    public virtual async Task<ProgramDetailsDto> GetProgramDetailsAsync()
    {
        var tenant = await GetCurrentTenantAsync();

        return new ProgramDetailsDto
        {
            DisplayName = GetExtraPropertyValue(tenant, "DisplayName"),
            Division = GetExtraPropertyValue(tenant, "Division"),
            Branch = GetExtraPropertyValue(tenant, "Branch"),
            Description = GetExtraPropertyValue(tenant, "Description")
        };
    }

    public virtual async Task UpdateProgramDetailsAsync(UpdateProgramDetailsDto input)
    {
        var tenant = await GetCurrentTenantAsync();

        tenant.SetProperty("DisplayName", NormalizeProgramDetail(input.DisplayName));
        tenant.SetProperty("Division", NormalizeProgramDetail(input.Division));
        tenant.SetProperty("Branch", NormalizeProgramDetail(input.Branch));
        tenant.SetProperty("Description", NormalizeProgramDetail(input.Description));

        using (CurrentTenant.Change(null))
        {
            await tenantRepository.UpdateAsync(tenant);
        }
    }

    protected virtual async Task<Tenant> GetCurrentTenantAsync()
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new AbpAuthorizationException("Program details can only be managed within a tenant context.");
        }

        var tenantId = CurrentTenant.Id.Value;
        using (CurrentTenant.Change(null))
        {
            return await tenantRepository.FindAsync(tenantId) ?? throw new EntityNotFoundException(typeof(Tenant), tenantId);
        }
    }

    protected virtual string GetExtraPropertyValue(Tenant tenant, string propertyName)
    {
        return tenant.GetProperty(propertyName)?.ToString() ?? string.Empty;
    }

    protected virtual string NormalizeProgramDetail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
