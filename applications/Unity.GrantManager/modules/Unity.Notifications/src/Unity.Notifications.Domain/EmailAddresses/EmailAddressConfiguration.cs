using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.EmailAddresses;

public class EmailAddressConfiguration : AuditedAggregateRoot<Guid>, IMultiTenant
{
    protected EmailAddressConfiguration()
    {
    }

    public EmailAddressConfiguration(Guid id, string emailAddress, string emailType, string description)
        : base(id)
    {
        EmailAddress = emailAddress;
        EmailType = emailType;
        Description = description;
        IsActive = true;
    }

    public Guid? TenantId { get; protected set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string EmailType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
