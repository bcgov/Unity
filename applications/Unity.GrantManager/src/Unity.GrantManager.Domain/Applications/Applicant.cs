using System;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class Applicant : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public string? ApplicantName { get; set; }
    public string? NonRegisteredBusinessName { get; set; }
    public string? OrgName { get; set; }
    public string? OrgNumber { get; set; }
    public string? OrgStatus { get; set; }
    public string? OrganizationType { get; set; }
    public string? OrganizationSize { get; set; }
    public string? Sector { get; set; }
    public string? SubSector { get; set; }
    public string? Status { get; set; }
    public string? ApproxNumberOfEmployees { get; set; }
    public string? IndigenousOrgInd { get; set; }
    public string? SectorSubSectorIndustryDesc { get; set; }
    public Guid? TenantId { get; set; }
    public virtual Collection<ApplicantAddress>? ApplicantAddresses { get; set; }
}
