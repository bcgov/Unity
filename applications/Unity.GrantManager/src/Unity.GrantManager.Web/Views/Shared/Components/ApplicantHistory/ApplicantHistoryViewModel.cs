using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantHistory;

public class ApplicantHistoryViewModel
{
    public Guid ApplicantId { get; set; }
    public string? FundingHistoryComments { get; set; }
    public string? IssueTrackingComments { get; set; }
    public string? AuditComments { get; set; }
}
