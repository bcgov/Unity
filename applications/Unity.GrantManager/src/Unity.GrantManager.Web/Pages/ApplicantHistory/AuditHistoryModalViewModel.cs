using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class AuditHistoryModalViewModel
{
    [HiddenInput]
    public Guid ApplicantId { get; set; }

    [DisplayName("Audit Tracking Number")]
    public string? AuditTrackingNumber { get; set; }

    [DisplayName("Audit Date")]
    [DataType(DataType.Date)]
    public DateTime? AuditDate { get; set; }

    [DisplayName("Audit Note")]
    public string? AuditNote { get; set; }
}
