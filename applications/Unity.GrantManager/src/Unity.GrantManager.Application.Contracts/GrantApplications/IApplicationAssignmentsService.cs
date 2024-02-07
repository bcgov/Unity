using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IApplicationAssignmentsService : IApplicationService
{
  
    Task<IList<GrantApplicationAssigneeDto>> GetListWithApplicationIdsAsync(List<Guid> ids);

}
