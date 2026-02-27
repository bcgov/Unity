using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicantProfile;

public class AuditHistoryDto : AuditedEntityDto<Guid>
{
    public Guid? ApplicantId { get; set; }
    public string? AuditTrackingNumber { get; set; }
    public DateTime? AuditDate { get; set; }
    public string? AuditNote { get; set; }
}
