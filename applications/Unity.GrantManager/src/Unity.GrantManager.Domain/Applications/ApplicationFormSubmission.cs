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
    private Guid _applicationId = Guid.Empty;    
    public Guid ApplicationId 
    { 
        get => _applicationId; 
        set
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("ApplicationId cannot be an empty GUID.");
            }
            _applicationId = value;
        }
    }

    public string ChefsSubmissionGuid { get; set; } = string.Empty;
    [Column(TypeName = "jsonb")]
    public string Submission { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? RenderedHTML { get; set; } = string.Empty;
    public Guid? FormVersionId { get; set; }
    [Column(TypeName = "jsonb")]
    public string ReportData { get; set; } = "{}";
    public Guid? ApplicationFormVersionId { get; set; }
}
