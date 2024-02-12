using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.GrantApplications;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationAssignmentsAppService), typeof(IApplicationAssignmentsService))]
public class ApplicationAssignmentsAppService : ApplicationService, IApplicationAssignmentsService
{
    private readonly IApplicationAssignmentRepository _applicationAssignmentsRepository;
    public ApplicationAssignmentsAppService(IApplicationAssignmentRepository repository)
    {
        _applicationAssignmentsRepository = repository;
    }

    public async Task<IList<GrantApplicationAssigneeDto>> GetListWithApplicationIdsAsync(List<Guid> ids)
    {
        var assignments = await _applicationAssignmentsRepository.GetListAsync(e => ids.Contains(e.ApplicationId));

        return ObjectMapper.Map<List<ApplicationAssignment>, List<GrantApplicationAssigneeDto>>(assignments.OrderBy(t => t.Id).ToList());
    }

   
}