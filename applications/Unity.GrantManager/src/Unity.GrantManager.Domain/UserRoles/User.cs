using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.ApplicationUserRoles;

public class User : AuditedAggregateRoot<Guid>
{
    public string OidcSub { get; set; } = string.Empty;
    public string OidcDisplayName { get; set; } = string.Empty;
    public string OidcEmail { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string PreferredFirstName { get; set; } = string.Empty;
    public string PreferredLastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
