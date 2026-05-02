using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Pages.ApplicantHistory;

public class ReportsHistoryModalViewModel
{
    [HiddenInput]
    public Guid ApplicantId { get; set; }

    [DisplayName("Fiscal Year")]
    public string? FiscalYear { get; set; }

    [DisplayName("Report Date")]
    [DataType(DataType.Date)]
    public DateTime? ReportDate { get; set; }

    [DisplayName("Outstanding")]
    public bool? Outstanding { get; set; }

    [DisplayName("Incomplete Report")]
    public bool? IncompleteReport { get; set; }

    [DisplayName("Note")]
    public string? Note { get; set; }
}
