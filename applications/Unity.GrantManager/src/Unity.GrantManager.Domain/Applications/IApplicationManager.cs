using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Applications;
public interface IApplicationManager
{
    Task AssignUserAsync(Guid applicationId, Guid assigneeId, string? duty);
    Task UpdateAssigneeAsync(Guid applicationId, Guid assigneeId, string? duty);
    Task RemoveAssigneeAsync(Guid applicationId, Guid assigneeId);
    Task<List<ApplicationActionResultItem>> GetActions(Guid applicationId);    
    Task<Application> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction);
    Task SetAssigneesAsync(Guid applicationId, List<(Guid? assigneeId, string? fullName)> assigneeSubs);
    bool IsActionAllowed(Application application, GrantApplicationAction triggerAction);
}
