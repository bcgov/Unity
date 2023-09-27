using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.ApplicationUserRoles;

public class UserTeam : AuditedAggregateRoot<Guid>
{
    public Guid TeamId { get; set; }

    public string OidcSub { get; set; } = string.Empty;
    
}
