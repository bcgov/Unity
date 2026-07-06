using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.GrantApplications;

public class UpdateApplicantPortalProgramDetailsDto
{
    [StringLength(ApplicantPortalProgramDetailsConsts.MaxDisplayNameLength)]
    public string? DisplayName { get; set; }

    [StringLength(ApplicantPortalProgramDetailsConsts.MaxDivisionLength)]
    public string? Division { get; set; }

    [StringLength(ApplicantPortalProgramDetailsConsts.MaxBranchLength)]
    public string? Branch { get; set; }

    [StringLength(ApplicantPortalProgramDetailsConsts.MaxDescriptionLength)]
    public string? Description { get; set; }
}
