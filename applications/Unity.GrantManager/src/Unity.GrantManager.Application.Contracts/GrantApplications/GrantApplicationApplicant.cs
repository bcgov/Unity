using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationApplicantDto : AuditedEntityDto<Guid>
{
    public string ApplicantName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string SubSector { get; set; } = string.Empty;
    public string OrgNumber { get; set; } = string.Empty;
    public string OrgName  { get; set; } = string.Empty;
    public string OrgStatus { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string OrganizationSize { get; set; } = string.Empty;
    public string SectorSubSectorIndustryDesc { get; set; } = string.Empty;
}
