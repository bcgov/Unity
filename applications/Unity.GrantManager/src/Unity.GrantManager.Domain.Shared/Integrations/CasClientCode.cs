using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Integrations;

public class CasClientCode : AuditedEntity<Guid>
{
    public string ClientCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MinistryPrefix { get; set; } = string.Empty;
    public string? FinancialMinistry { get; set; }
    public string? ClientId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastUpdatedTime { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
