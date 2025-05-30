using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationForm : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid IntakeId { get; set; }
    [Required]
    public string? ApplicationFormName { get; set; }
    public string? ApplicationFormDescription { get; set; }
    public string? ChefsApplicationFormGuid { get; set; }
    public string? ChefsCriteriaFormGuid { get; set; }
    public string? ApiKey { get; set; }
    public string? AvailableChefsFields { get; set; }
    public int? Version { get; set; }
    public string? Category { get; set; }
    public string? ConnectionHttpStatus { get; set; }
    public DateTime? AttemptedConnectionDate { get; set; }
    public bool Payable {  get; set; }
    public Guid? ScoresheetId {  get; set; }
    public Guid? TenantId { get; set; }
    public bool RenderFormIoToHtml { get; set; } = false;
    public bool IsDirectApproval { get; set; } = false;
    
}
