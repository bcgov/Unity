using System;

namespace Unity.GrantManager.ApplicantProfile;

public class CreateUpdateAuditHistoryDto
{
    public Guid? ApplicantId { get; set; }
    public string? AuditTrackingNumber { get; set; }
    public DateTime? AuditDate { get; set; }
    public string? AuditNote { get; set; }
}
