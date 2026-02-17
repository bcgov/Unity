using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace Unity.GrantManager.Integrations;

public class CasClientCode : FullAuditedAggregateRoot<Guid>
{
    [MaxLength(3)]
    public string ClientCode { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(3)]
    public string MinistryPrefix { get; set; } = string.Empty;
    
    public string? FinancialMinistry { get; set; }
    public string? ClientId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastUpdatedTime { get; set; }
}
