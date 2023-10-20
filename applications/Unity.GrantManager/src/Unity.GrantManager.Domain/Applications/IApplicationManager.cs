using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Applications;
public interface IApplicationManager
{
    Task AssignUserAsync(Guid applicationId, string oidcSub, string assigneeDisplayName);
    Task RemoveAssigneeAsync(Guid applicationId, string oidcSub);
    Task<List<ApplicationActionResultItem>> GetActions(Guid applicationId);    
    Task<Application> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction);
    Task SetAssigneesAsync(Guid applicationId, List<(string oidcSub, string displayName)> oidcSubs);
}
