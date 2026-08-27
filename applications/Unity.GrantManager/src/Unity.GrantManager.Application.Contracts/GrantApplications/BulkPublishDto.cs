using System;

namespace Unity.GrantManager.GrantApplications;

public class BulkPublishDto
{
    public BulkPublishDto()
    {
        ReferenceNo = string.Empty;
        ApplicantName = string.Empty;
        FormName = string.Empty;
        ApplicationStatus = string.Empty;
    }

    public Guid ApplicationId { get; set; }
    public string ReferenceNo { get; set; }
    public string ApplicantName { get; set; }
    public string FormName { get; set; }
    public DateTime? FinalDecisionDate { get; set; }
    public string ApplicationStatus { get; set; }
    public bool ExternalStatusVisibility { get; set; }
    public string ExternalStatus { get; set; } = string.Empty;
    public string PublishedStatus { get; set; } = string.Empty;
}
