using System;
using System.ComponentModel;

namespace Unity.GrantManager.Web.Pages.BulkActions;

public class BulkPublishApplicationViewModel
{
    public Guid ApplicationId { get; set; }
    public string? ApplicantName { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public bool ExternalStatusVisibility { get; set; }
    public string ApplicationStatus { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;

    [DisplayName("External Status (Current)")]
    public string ExternalStatus { get; set; } = string.Empty;
    
    [DisplayName("Published Status (Future)")]
    public string PublishedStatus { get; set; } = string.Empty;

    [DisplayName("Decision Date")]
    public DateTime? FinalDecisionDate { get; set; }
    public bool IsValid { get; set; }
}
