using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Entities;

namespace Unity.TenantManagement;

[Controller]
[RemoteService(Name = TenantManagementRemoteServiceConsts.RemoteServiceName)]
[Area(TenantManagementRemoteServiceConsts.ModuleName)]
[Route("api/multi-tenancy/tenants")]
public class TenantController : AbpControllerBase, ITenantAppService
{
    protected ITenantAppService TenantAppService { get; }

    public TenantController(ITenantAppService tenantAppService)
    {
        TenantAppService = tenantAppService;
    }

    [HttpGet]
    [Route("{id}")]
    public virtual async Task<IActionResult> GetAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid model state.");
        }

        var tenant = await TenantAppService.GetAsync(id);
        if (tenant == null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetListAsync(GetTenantsInput input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid input.");
        }

        var tenants = await TenantAppService.GetListAsync(input);
        return Ok(tenants);
    }

    [HttpPost]
    public virtual async Task<IActionResult> CreateAsync(TenantCreateDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return BadRequest with validation errors
        }

        var tenant = await TenantAppService.CreateAsync(input);

        return Ok(tenant);
    }

    [HttpPut]
    [Route("{id}")]
    public virtual async Task<IActionResult> UpdateAsync(Guid id, TenantUpdateDto input)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return BadRequest with validation errors
        }
        var tenant = await TenantAppService.UpdateAsync(id, input);
        return Ok(tenant);
    }

    [HttpDelete]
    [Route("{id}")]
    public virtual async Task<IActionResult> DeleteAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return BadRequest if model state is invalid
        }

        try
        {
            await TenantAppService.DeleteAsync(id);
            return NoContent(); // Return 204 No Content on successful deletion
        }
        catch (EntityNotFoundException)
        {
            return NotFound(); // Return 404 if the tenant is not found
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}"); // Return 500 for any other exceptions
        }
    }

    [HttpGet]
    [Route("{id}/default-connection-string")]
    public virtual async Task<IActionResult> GetDefaultConnectionStringAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState); // Return BadRequest with validation errors
        }
        var connectionString = await TenantAppService.GetDefaultConnectionStringAsync(id);
        return Ok(connectionString);
    }

    [HttpPut]
    [Route("{id}/default-connection-string")]
    public virtual async Task<IActionResult> UpdateDefaultConnectionStringAsync(Guid id, string defaultConnectionString)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await TenantAppService.UpdateDefaultConnectionStringAsync(id, defaultConnectionString);
        return Ok(); 
    }

    [HttpDelete]
    [Route("{id}/default-connection-string")]
    public virtual async Task<IActionResult> DeleteDefaultConnectionStringAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await TenantAppService.DeleteDefaultConnectionStringAsync(id);
        return Ok();
    }

    [HttpPut]
    [Route("{id}/assign-manager")]
    public virtual async Task<IActionResult> AssignManagerAsync(TenantAssignManagerDto managerAssignment)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await TenantAppService.AssignManagerAsync(managerAssignment);
        return Ok();
    }

}
