using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.GrantApplications;

public class ComposeEmailApplicationDto
{
    public Guid ApplicationId { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public decimal ApprovedAmount { get; set; }
    public DateTime? DecisionDate { get; set; }
}

public class ComposeBulkEmailRequestDto
{
    public List<ComposedEmailDto> Emails { get; set; } = [];
}

public class ComposedEmailDto
{
    public Guid ApplicationId { get; set; }

    [Required]
    public string EmailTo { get; set; } = string.Empty;

    public string? EmailCC { get; set; }
    public string? EmailBCC { get; set; }

    [Required]
    public string EmailFrom { get; set; } = string.Empty;

    [Required]
    [MaxLength(1023)]
    public string EmailSubject { get; set; } = string.Empty;

    [Required]
    public string EmailBody { get; set; } = string.Empty;

    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
}
