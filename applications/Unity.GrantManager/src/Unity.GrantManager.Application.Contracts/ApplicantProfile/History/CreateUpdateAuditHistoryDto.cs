using System;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.ApplicantProfile;

public class CreateUpdateAuditHistoryDto
{
    public Guid? ApplicantId { get; set; }
    public string? AuditTrackingNumber { get; set; }
    public DateTime? AuditDate { get; set; }
    public AuditHistoryStatus? AuditStatus { get; set; }
    public string? AuditorName { get; set; }
    public string? AuditNote { get; set; }
}
