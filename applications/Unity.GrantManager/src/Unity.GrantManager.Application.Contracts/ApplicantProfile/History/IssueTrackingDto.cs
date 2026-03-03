using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicantProfile;

public class IssueTrackingDto : AuditedEntityDto<Guid>
{
    public Guid? ApplicantId { get; set; }
    public int? Year { get; set; }
    public string? IssueHeading { get; set; }
    public string? IssueDescription { get; set; }
    public bool? Resolved { get; set; }
    public string? ResolutionNote { get; set; }
}
