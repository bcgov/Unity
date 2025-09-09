using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Applicants;

public class ApplicantListDto : AuditedEntityDto<Guid>
{
    // Default visible columns
    public string? ApplicantName { get; set; }
    public string? UnityApplicantId { get; set; }
    public string? OrgName { get; set; }
    public string? OrgNumber { get; set; }
    public string? OrgStatus { get; set; }
    public string? OrganizationType { get; set; }
    public string? Status { get; set; }
    public bool? RedStop { get; set; }
    
    // Additional columns (initially hidden)
    public string? NonRegisteredBusinessName { get; set; }
    public string? NonRegOrgName { get; set; }
    public string? OrganizationSize { get; set; }
    public string? Sector { get; set; }
    public string? SubSector { get; set; }
    public string? ApproxNumberOfEmployees { get; set; }
    public string? IndigenousOrgInd { get; set; }
    public string? SectorSubSectorIndustryDesc { get; set; }
    public string? FiscalMonth { get; set; }
    public string? BusinessNumber { get; set; }
    public int? FiscalDay { get; set; }
    public DateTime? StartedOperatingDate { get; set; }
    public string? SupplierId { get; set; }
    public Guid? SiteId { get; set; }
    public decimal? MatchPercentage { get; set; }
    public bool? IsDuplicated { get; set; }
    public string? ElectoralDistrict { get; set; }
    
    // Calculated fields for display
    public int ApplicationCount { get; set; }
    public DateTime? LastApplicationDate { get; set; }
}