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
    // Retained to protect the DB column from being dropped by EF Core migrations.
    // No longer read or written by Unity — ApproxNumberOfEmployees is the canonical field.
    // Do not remove until a DB migration explicitly drops or consolidates the column.
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
    public DateOnly? StartedOperatingDate { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? SupplierId { get; set; }
    public virtual Collection<ApplicantAddress>? ApplicantAddresses { get; set; }
    public decimal? MatchPercentage { get; set; }
    public string? NonRegOrgName { get; set; }
    public bool IsDuplicated { get; set; }
    public string? FundingHistoryComments { get; set; }
    public string? IssueTrackingComments { get; set; }
    public string? AuditComments { get; set; }
    public string? ReportsComments { get; set; }
    // Soft-delete fields are declared manually instead of implementing ABP's ISoftDelete.
    // ISoftDelete would register a global EF Core query filter that applies to Include() joins —
    // Application.Applicant is a required navigation (OnDelete NoAction), so the filter would
    // silently drop Application rows whose Applicant is soft-deleted, breaking the application
    // and payment list pages. Filtering is applied explicitly only where needed (applicant list,
    // lookup, autocomplete) via .Where(a => !a.IsDeleted).
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
    public Guid? DeleterId { get; set; }
}
