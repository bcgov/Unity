using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Applicants;

public class MergeApplicantsDto
{
    public Guid PrincipalApplicantId { get; set; }
    public Guid SecondaryApplicantId { get; set; }

    [Required]
    public ApplicantMergeSummaryDto Summary { get; set; } = new();

    public Guid? SelectedSupplierId { get; set; }
    public ApplicantMergeSource Source { get; set; }
}

public class ApplicantMergeSummaryDto
{
    public string? ApplicantName { get; set; }
    public string? UnityApplicantId { get; set; }
    public string? OrgName { get; set; }
    public string? OrgNumber { get; set; }
    public string? NonRegOrgName { get; set; }
    public string? OrganizationType { get; set; }
    public string? ApproxNumberOfEmployees { get; set; }
    public string? OrgStatus { get; set; }
    public string? IndigenousOrgInd { get; set; }
    public string? Sector { get; set; }
    public string? SubSector { get; set; }
    public string? SectorSubSectorIndustryDesc { get; set; }
    public int? FiscalDay { get; set; }
    public string? FiscalMonth { get; set; }
}

public class UnmergeApplicantsDto
{
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public class ApplicantMergeDto
{
    public Guid Id { get; set; }
    public Guid PrincipalApplicantId { get; set; }
    public Guid SecondaryApplicantId { get; set; }
    public ApplicantMergeStatus Status { get; set; }
    public ApplicantMergeSource Source { get; set; }
    public DateTime MergedAt { get; set; }
    public Guid? MergedById { get; set; }
    public DateTime? ReversedAt { get; set; }
    public Guid? ReversedById { get; set; }
    public string? ReversalReason { get; set; }
    public int TransferredApplicationCount { get; set; }
}

public class ApplicantMergePreviewDto : ApplicantMergeDto
{
    public string PrincipalApplicantName { get; set; } = string.Empty;
    public string SecondaryApplicantName { get; set; } = string.Empty;
    public bool CanUnmerge { get; set; }
    public string? BlockReason { get; set; }
}

public class ApplicantMergeListDto
{
    public List<ApplicantMergePreviewDto> Items { get; set; } = [];
}
