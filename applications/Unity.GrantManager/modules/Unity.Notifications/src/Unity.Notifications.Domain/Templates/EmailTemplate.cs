using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.Notifications.Templates;

public class EmailTemplate : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    protected EmailTemplate()
    {
        /* This constructor is for ORMs to be used while getting the entity from the database. */
    }

    public EmailTemplate(Guid id,
        string name,
        string description,
        string subject,
        string bodyText,
        string bodyHTML,
        string sendFrom)
        : base(id)
    {
        Name = name;
        Description = description;
        Subject = subject;
        BodyText = bodyText;
        BodyHTML = bodyHTML;
        SendFrom = sendFrom;
    }

    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    public string BodyText { get; set; } = string.Empty;
    public string BodyHTML { get; set; } = string.Empty;
    public string SendFrom { get; set; } = string.Empty;
}
