using System;

namespace Unity.GrantManager.ApplicantProfile;

public class CreateUpdateReportsHistoryDto
{
    public Guid? ApplicantId { get; set; }
    public string? FiscalYear { get; set; }
    public DateTime? ReportDate { get; set; }
    public bool? Outstanding { get; set; }
    public bool? IncompleteReport { get; set; }
    public string? Note { get; set; }
}
