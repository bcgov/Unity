using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationAssignmentsService : IApplicationService
{  
    Task<List<GrantApplicationAssigneeDto>> GetListWithApplicationIdsAsync(List<Guid> ids);
    Task InsertAssigneeAsync(Guid applicationId, Guid assigneeId, string? duty);
    Task DeleteAssigneeAsync(Guid applicationId, Guid assigneeId);    
}
