using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.ApplicationUserRoles;

public class Team : AuditedAggregateRoot<Guid>
{
   
    public string Description { get; set; } = string.Empty;
}
