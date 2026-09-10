using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Identity
{
    public interface IHostRoleAppService : IApplicationService
    {
        Task<List<HostRoleAssignmentDto>> GetListAsync();
        Task AssignRoleAsync(AssignHostRoleDto input);
        Task RevokeRoleAsync(Guid userId, string roleName);
    }
}
