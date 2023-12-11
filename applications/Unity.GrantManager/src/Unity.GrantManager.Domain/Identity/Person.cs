using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Identity;

public class Person : AuditedAggregateRoot<Guid>
{
    public new Guid Id { get => base.Id; set => base.Id = value; }
    public string OidcSub { get; set; } = string.Empty;
    public string OidcDisplayName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
}
