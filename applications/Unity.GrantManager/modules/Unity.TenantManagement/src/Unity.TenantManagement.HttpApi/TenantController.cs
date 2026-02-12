using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.TenantManagement;

[Controller]
[RemoteService(Name = TenantManagementRemoteServiceConsts.RemoteServiceName)]
[Area(TenantManagementRemoteServiceConsts.ModuleName)]
[Route("api/multi-tenancy/tenants")]
public class TenantController(ITenantAppService tenantAppService) : AbpControllerBase, ITenantAppService
{
    protected ITenantAppService TenantAppService { get; } = tenantAppService;

    [HttpGet]
    [Route("{id}")]
    public virtual Task<TenantDto> GetAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->GetAsync: ModelState Invalid");
        }
        return TenantAppService.GetAsync(id);
    }

    [HttpGet]
    public virtual Task<PagedResultDto<TenantDto>> GetListAsync(GetTenantsInput input)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->GetListAsync: ModelState Invalid");
        }
        return TenantAppService.GetListAsync(input);
    }

    [HttpPost]
    public virtual Task<TenantDto> CreateAsync(TenantCreateDto input)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->CreateAsync: ModelState Invalid");
        }
        return TenantAppService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public virtual Task<TenantDto> UpdateAsync(Guid id, TenantUpdateDto input)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->UpdateAsync: ModelState Invalid");
        }
        return TenantAppService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->DeleteAsync: ModelState Invalid");
        }
        return TenantAppService.DeleteAsync(id);
    }

    [HttpGet]
    [Route("{id}/default-connection-string")]
    public virtual Task<string> GetDefaultConnectionStringAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->GetDefaultConnectionStringAsync: ModelState Invalid");
        }
        return TenantAppService.GetDefaultConnectionStringAsync(id);
    }

    [HttpPut]
    [Route("{id}/default-connection-string")]
    public virtual Task UpdateDefaultConnectionStringAsync(Guid id, string defaultConnectionString)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->UpdateDefaultConnectionStringAsync: ModelState Invalid");
        }
        return TenantAppService.UpdateDefaultConnectionStringAsync(id, defaultConnectionString);
    }

    [HttpDelete]
    [Route("{id}/default-connection-string")]
    public virtual Task DeleteDefaultConnectionStringAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->DeleteDefaultConnectionStringAsync: ModelState Invalid");
        }
        return TenantAppService.DeleteDefaultConnectionStringAsync(id);
    }

    [HttpPut]
    [Route("assign-manager")]
    public virtual Task AssignManagerAsync(TenantAssignManagerDto managerAssignment)
    {
        if (!ModelState.IsValid)
        {
            throw new UserFriendlyException("TenantController->AssignManagerAsync: ModelState Invalid");
        }
        return TenantAppService.AssignManagerAsync(managerAssignment);
    }

    public Task<string> GetCurrentTenantCasClientClientCode(Guid tenantId)
    {
        throw new NotImplementedException();
    }
}
