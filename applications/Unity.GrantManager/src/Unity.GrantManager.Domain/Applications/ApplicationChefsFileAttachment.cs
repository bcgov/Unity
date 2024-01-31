using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Applications;

public class ApplicationChefsFileAttachment : AuditedAggregateRoot<Guid>
{
    public Guid ApplicationId { get; set; }
    public string? ChefsSumbissionId { get; set; }
    public string? ChefsFileId { get; set; }
    public string? Name { get; set; }
}
