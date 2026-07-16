using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.TenantManagement;

public interface ITenantAppService : ICrudAppService<TenantDto, Guid, GetTenantsInput, TenantCreateDto, TenantUpdateDto>
{
    Task AssignManagerAsync(TenantAssignManagerDto managerAssignment);
    Task<TenantConnectionStringsDto> GetConnectionStringsAsync(Guid id);
    Task UpdateConnectionStringsAsync(Guid id, TenantConnectionStringsDto input);
    Task<List<TenantManagerDto>> GetManagersAsync(Guid id);
}
