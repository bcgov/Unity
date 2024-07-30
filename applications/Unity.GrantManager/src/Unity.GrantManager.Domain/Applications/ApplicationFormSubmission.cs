using System;
using Volo.Abp.Domain.Entities.Auditing;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationFormSubmission : AuditedAggregateRoot<Guid>, IMultiTenant
{    
    public string OidcSub { get; set; } = string.Empty;
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicationId { get; set; } = Guid.Empty; // TODO : harden this contraint
    public string ChefsSubmissionGuid { get; set; } = string.Empty;
    [Column(TypeName = "jsonb")]
    public string Submission { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? RenderedHTML { get; set; } = string.Empty;
    public Guid? FormVersionId { get; set; }
}
