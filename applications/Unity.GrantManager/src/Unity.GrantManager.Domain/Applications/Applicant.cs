using System;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class Applicant : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public string ApplicantName { get; set; } = string.Empty;
    public string NonRegisteredBusinessName { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string OrgNumber { get; set; } = string.Empty;
    public string OrgStatus { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string OrganizationSize { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string SubSector { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ApproxNumberOfEmployees { get; set; } = string.Empty;
    public string IndigenousOrgInd { get; set; } = string.Empty;
    public string SectorSubSectorIndustryDesc { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }

    public virtual Collection<ApplicantAddress>? ApplicantAddresses { get; set; }
}
