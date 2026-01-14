using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Assignments;

[Authorize]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationAssignmentsAppService), typeof(IApplicationAssignmentsService))]
public class ApplicationAssignmentsAppService(IApplicationManager applicationManager,
    IPersonRepository personRepository,
    IApplicationAssignmentRepository applicationAssignmentRepository)
    : ApplicationService, IApplicationAssignmentsService
{
    public async Task<List<GrantApplicationAssigneeDto>> GetListWithApplicationIdsAsync(List<Guid> ids)
    {
        var assignments = await applicationAssignmentRepository.GetListAsync(e => ids.Contains(e.ApplicationId));

        return ObjectMapper.Map<List<ApplicationAssignment>, List<GrantApplicationAssigneeDto>>(assignments.OrderBy(t => t.Id).ToList());
    }

    public async Task InsertAssigneeAsync(Guid applicationId, Guid assigneeId, string? duty)
    {
        try
        {
            var assignees = await GetAssigneesAsync(applicationId);
            if (assignees == null || assignees.FindIndex(a => a.AssigneeId == assigneeId) == -1)
            {
                await applicationManager.AssignUserAsync(applicationId, assigneeId, duty);
            }
            else
            {
                await applicationManager.UpdateAssigneeAsync(applicationId, assigneeId, duty);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    public async Task DeleteAssigneeAsync(Guid applicationId, Guid assigneeId)
    {
        try
        {
            await applicationManager.RemoveAssigneeAsync(applicationId, assigneeId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private async Task<List<GrantApplicationAssigneeDto>> GetAssigneesAsync(Guid applicationId)
    {
        var query = from userAssignment in await applicationAssignmentRepository.GetQueryableAsync()
                    join user in await personRepository.GetQueryableAsync() on userAssignment.AssigneeId equals user.Id
                    where userAssignment.ApplicationId == applicationId
                    select new GrantApplicationAssigneeDto
                    {
                        Id = userAssignment.Id,
                        AssigneeId = userAssignment.AssigneeId,
                        FullName = user.FullName,
                        Duty = userAssignment.Duty,
                        ApplicationId = applicationId
                    };

        return await query.ToListAsync();
    }
}