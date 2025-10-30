using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationApplicantDto : AuditedEntityDto<Guid>
{
    public string ApplicantName { get; set; } = string.Empty;
    public Guid SiteId { get; set; } = Guid.Empty;
    public Guid SupplierId { get; set; } = Guid.Empty;
    public string Sector { get; set; } = string.Empty;
    public string SubSector { get; set; } = string.Empty;
    public string OrgNumber { get; set; } = string.Empty;
    public string OrgName  { get; set; } = string.Empty;
    public string OrgStatus { get; set; } = string.Empty;
    public string BusinessNumber { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string OrganizationSize { get; set; } = string.Empty;
    public string SectorSubSectorIndustryDesc { get; set; } = string.Empty;
    public bool RedStop { get; set; } = false;
    public string IndigenousOrgInd { get; set; } = string.Empty;
    public string UnityApplicantId { get; set; } = string.Empty;
    public string FiscalDay { get; set; } = string.Empty;
    public string FiscalMonth { get; set; } = string.Empty;
    public string? ElectoralDistrict { get; set; }
}
