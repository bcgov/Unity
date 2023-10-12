using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Applications;
public interface IApplicationManager
{
    Task<List<ApplicationActionResultItem>> GetActions(Guid applicationId);
    Task<Application> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction);
}
