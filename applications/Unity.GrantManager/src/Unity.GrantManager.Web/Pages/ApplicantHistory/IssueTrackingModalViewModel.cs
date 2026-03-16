using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class IssueTrackingModalViewModel
{
    [HiddenInput]
    public Guid ApplicantId { get; set; }

    [DisplayName("Year")]
    public int? Year { get; set; }

    [DisplayName("Issue Heading")]
    public string? IssueHeading { get; set; }

    [DisplayName("Issue Description")]
    public string? IssueDescription { get; set; }

    [DisplayName("Resolved")]
    public bool? Resolved { get; set; }

    [DisplayName("Resolution Note")]
    public string? ResolutionNote { get; set; }
}
