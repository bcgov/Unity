namespace Unity.GrantManager.GrantApplications;

public class UpdateApplicantSummaryDto
{
    public string? ApplicantName { get; set; }
    public string? Sector { get; set; }
    public string? SubSector { get; set; }
    public string? OrgNumber { get; set; }
    public string? OrgName { get; set; }
    public string? NonRegOrgName { get; set; }
    public string? OrgStatus { get; set; }
    public string? BusinessNumber { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationSize { get; set; }
    public string? SectorSubSectorIndustryDesc { get; set; }
    public bool? RedStop { get; set; }
    public bool? IndigenousOrgInd { get; set; }
    public string? UnityApplicantId { get; set; }
    public string? FiscalDay { get; set; }
    public string? FiscalMonth { get; set; }    
}
