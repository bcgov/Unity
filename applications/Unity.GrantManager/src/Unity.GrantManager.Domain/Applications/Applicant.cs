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
    public bool? RedStop { get; set; }
    public string? UnityApplicantId { get; set; }
    public string? FiscalMonth { get; set; }
    public string? BusinessNumber { get; set; }
    public int? FiscalDay { get; set; }
    public string? OrganizationOperationLength { get; set; }
    public DateOnly? StartedOperatingDate { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? SiteId { get; set; }
    public virtual Collection<ApplicantAddress>? ApplicantAddresses { get; set; }
    public decimal? MatchPercentage { get; set; }
    public string? NonRegOrgName { get; set; }
    public bool? IsDuplicated { get; set; }   
}
