using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationStatusAppService), typeof(IApplicationStatusService))]
public class ApplicationStatusAppService : ApplicationService, IApplicationStatusService
{
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly ITenantRepository _tenantRepository;

    public ApplicationStatusAppService(
        IApplicationStatusRepository repository,
        ITenantRepository tenantRepository)
    {
        _applicationStatusRepository = repository;
        _tenantRepository = tenantRepository;
    }

    public virtual async Task<IList<ApplicationStatusDto>> GetListAsync()
    {        
        var statuses = await _applicationStatusRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationStatus>, List<ApplicationStatusDto>>(statuses.OrderBy(s => s.StatusCode).ToList());
    }

    public virtual async Task<IList<ApplicantPortalStatusDto>> GetApplicantPortalStatusListAsync()
    {
        var statuses = await _applicationStatusRepository.GetListAsync();

        return ObjectMapper.Map<List<ApplicationStatus>, List<ApplicantPortalStatusDto>>(statuses.OrderBy(s => s.StatusCode).ToList());
    }

    [Authorize(UnitySettingManagementPermissions.EditProgramDetails)]
    public virtual async Task<ProgramDetailsDto> GetProgramDetailsAsync()
    {
        var tenant = await GetCurrentTenantForProgramDetailsAsync();

        return new ProgramDetailsDto
        {
            DisplayName = GetExtraPropertyValue(tenant, "DisplayName"),
            Division = GetExtraPropertyValue(tenant, "Division"),
            Branch = GetExtraPropertyValue(tenant, "Branch"),
            Description = GetExtraPropertyValue(tenant, "Description")
        };
    }

    public virtual async Task UpdateExternalStatusLabelsAsync(UpdateApplicationStatusExternalLabelsDto input)
    {
        // Load all statuses in a single query by IDs
        var statusIds = input.Statuses.Select(s => s.Id).ToList();
        var statuses = await _applicationStatusRepository.GetListAsync(s => statusIds.Contains(s.Id));
        var statusMap = statuses.ToDictionary(s => s.Id);

        foreach (var statusDto in input.Statuses)
        {
            if (statusMap.TryGetValue(statusDto.Id, out var status))
            {
                status.ExternalStatus = statusDto.ExternalStatus;
                status.NotifiedStatus = string.IsNullOrWhiteSpace(statusDto.NotifiedStatus) ? null : statusDto.NotifiedStatus;
                await _applicationStatusRepository.UpdateAsync(status);
            }
        }
    }

    [Authorize(UnitySettingManagementPermissions.EditProgramDetails)]
    public virtual async Task UpdateProgramDetailsAsync(UpdateProgramDetailsDto input)
    {
        var tenant = await GetCurrentTenantForProgramDetailsAsync();

        tenant.SetProperty("DisplayName", NormalizeProgramDetail(input.DisplayName));
        tenant.SetProperty("Division", NormalizeProgramDetail(input.Division));
        tenant.SetProperty("Branch", NormalizeProgramDetail(input.Branch));
        tenant.SetProperty("Description", NormalizeProgramDetail(input.Description));

        using (CurrentTenant.Change(null))
        {
            await _tenantRepository.UpdateAsync(tenant);
        }
    }

    protected virtual async Task<Tenant> GetCurrentTenantForProgramDetailsAsync()
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new AbpAuthorizationException("Program details can only be managed within a tenant context.");
        }

        var tenantId = CurrentTenant.Id.Value;
        using (CurrentTenant.Change(null))
        {
            return await _tenantRepository.FindAsync(tenantId) ?? throw new EntityNotFoundException(typeof(Tenant), tenantId);
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