using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationUserAssignment : AuditedAggregateRoot<Guid>
{
    public Guid? TeamId { get; set; }

    public string OidcSub { get; set; } = string.Empty;

    public Guid? ApplicationFormId { get; set; }
    public Guid ApplicationId { get; set; }

    public string AssigneeDisplayName { get; set; } = string.Empty;

    public DateTime AssignmentTime { get; set; }

}
