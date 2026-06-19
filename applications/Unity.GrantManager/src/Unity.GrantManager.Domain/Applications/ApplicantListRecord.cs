using System;

namespace Unity.GrantManager.Applications;

public class ApplicantListRecord
{
    public Guid Id { get; set; }
    public string? ApplicantName { get; set; }
    public string? UnityApplicantId { get; set; }
    public string? OrgName { get; set; }
    public string? OrgNumber { get; set; }
    public string? OrgStatus { get; set; }
    public string? OrganizationType { get; set; }
    public string? Status { get; set; }
    public bool? RedStop { get; set; }
    public string? NonRegisteredBusinessName { get; set; }
    public string? NonRegOrgName { get; set; }
    public string? Sector { get; set; }
    public string? SubSector { get; set; }
    public string? ApproxNumberOfEmployees { get; set; }
    public string? IndigenousOrgInd { get; set; }
    public string? SectorSubSectorIndustryDesc { get; set; }
    public string? FiscalMonth { get; set; }
    public string? BusinessNumber { get; set; }
    public int? FiscalDay { get; set; }
    public DateOnly? StartedOperatingDate { get; set; }
    public bool IsDuplicated { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
}
