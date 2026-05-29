using System;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicantProfile;

public class AuditHistoryDto : AuditedEntityDto<Guid>
{
    public Guid? ApplicantId { get; set; }
    public string? AuditTrackingNumber { get; set; }
    public DateTime? AuditDate { get; set; }
    public AuditHistoryStatus? AuditStatus { get; set; }
    public string? AuditorName { get; set; }
    public string? AuditNote { get; set; }
}
