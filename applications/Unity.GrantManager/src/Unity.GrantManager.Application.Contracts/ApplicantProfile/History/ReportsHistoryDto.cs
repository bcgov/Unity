using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicantProfile;

public class ReportsHistoryDto : AuditedEntityDto<Guid>
{
    public Guid? ApplicantId { get; set; }
    public string? FiscalYear { get; set; }
    public DateTime? ReportDate { get; set; }
    public bool? Outstanding { get; set; }
    public bool? SignedOff { get; set; }
    public bool? IncompleteReport { get; set; }
    public string? Note { get; set; }
}
