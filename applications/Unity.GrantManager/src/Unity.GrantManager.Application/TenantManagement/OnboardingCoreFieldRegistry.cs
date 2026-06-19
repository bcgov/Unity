#nullable enable
using System;
using System.Collections.Generic;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.TenantManagement;

public sealed record CoreFieldDefinition(string Key, string Label, string Type, string EfPath, Func<Application, object?> Selector)
{
    // Only string-shaped fields get server-side Contains filtering (matches how the existing
    // static columns filter — ToLower().Contains()). Currency/Number/Date fields are still
    // sortable, just not filterable yet — filtering on those would need a numeric/date-aware
    // operator instead of substring match.
    public bool IsTextFilterable => Type is "String" or "Email" or "Phone";
}

// Key/Label/Type mirror the [DisplayName]/[MapFieldType] attributes on
// Unity.GrantManager.Intakes.IntakeMapping. EfPath is the EF Core navigation path used for
// dynamic LINQ (System.Linq.Dynamic.Core) sorting/filtering — see
// GrantManagerOnboardingApplicationProvider.ApplySorting for the established pattern this
// mirrors (also used by ApplicationRepository.MapSortingField for the main Applications list).
// Only fields with a confirmed assignment in IntakeFormSubmissionManager.CreateNewApplicationAsync
// or ApplicantAppService.CreateOrRetrieveApplicantAsync/CreateApplicantAsync are listed here —
// several IntakeMapping fields (Contact*, Mailing*, most Physical*, OrgStatus, IndigenousOrgInd)
// are never persisted anywhere after intake and are intentionally omitted.
public static class OnboardingCoreFieldRegistry
{
    public static readonly IReadOnlyList<CoreFieldDefinition> Fields =
    [
        new("ProjectName", "Project Name", "String", "ProjectName", a => a.ProjectName),
        new("Acquisition", "Acquisition", "String", "Acquisition", a => a.Acquisition),
        new("Forestry", "Forestry", "String", "Forestry", a => a.Forestry),
        new("ForestryFocus", "Forestry Focus", "String", "ForestryFocus", a => a.ForestryFocus),
        new("PhysicalCity", "Physical City", "String", "City", a => a.City),
        new("EconomicRegion", "Economic Region", "String", "EconomicRegion", a => a.EconomicRegion),
        new("CommunityPopulation", "Community Population", "Number", "CommunityPopulation", a => a.CommunityPopulation),
        new("RequestedAmount", "Requested Amount", "Currency", "RequestedAmount", a => a.RequestedAmount),
        new("ProjectStartDate", "Project Start Date", "Date", "ProjectStartDate", a => a.ProjectStartDate),
        new("ProjectEndDate", "Project End Date", "Date", "ProjectEndDate", a => a.ProjectEndDate),
        new("TotalProjectBudget", "Total Project Budget", "Currency", "TotalProjectBudget", a => a.TotalProjectBudget),
        new("Community", "Community", "String", "Community", a => a.Community),
        new("ElectoralDistrict", "Project Electoral District", "String", "ElectoralDistrict", a => a.ElectoralDistrict),
        new("ApplicantElectoralDistrict", "Applicant Electoral District", "String", "ApplicantElectoralDistrict", a => a.ApplicantElectoralDistrict),
        new("RegionalDistrict", "Regional District", "String", "RegionalDistrict", a => a.RegionalDistrict),
        new("SigningAuthorityFullName", "Signing Authority Full Name", "String", "SigningAuthorityFullName", a => a.SigningAuthorityFullName),
        new("SigningAuthorityTitle", "Signing Authority Title", "String", "SigningAuthorityTitle", a => a.SigningAuthorityTitle),
        new("SigningAuthorityEmail", "Signing Authority Email", "Email", "SigningAuthorityEmail", a => a.SigningAuthorityEmail),
        new("SigningAuthorityBusinessPhone", "Signing Authority Business Phone", "Phone", "SigningAuthorityBusinessPhone", a => a.SigningAuthorityBusinessPhone),
        new("SigningAuthorityCellPhone", "Signing Authority Cell Phone", "Phone", "SigningAuthorityCellPhone", a => a.SigningAuthorityCellPhone),
        new("Place", "Place", "String", "Place", a => a.Place),
        new("RiskRanking", "Risk Ranking", "String", "RiskRanking", a => a.RiskRanking),
        new("ProjectSummary", "Project Summary", "String", "ProjectSummary", a => a.ProjectSummary),

        // Applicant-backed — requires `.Include(a => a.Applicant)` on the query.
        new("ApplicantName", "Applicant Name", "String", "Applicant.ApplicantName", a => a.Applicant.ApplicantName),
        new("NonRegisteredBusinessName", "Non-Registered Organization Name", "String", "Applicant.NonRegOrgName", a => a.Applicant.NonRegOrgName),
        new("OrgName", "Registered Organization Name", "String", "Applicant.OrgName", a => a.Applicant.OrgName),
        new("OrgNumber", "Registered Organization Number", "String", "Applicant.OrgNumber", a => a.Applicant.OrgNumber),
        new("BusinessNumber", "Business Number", "String", "Applicant.BusinessNumber", a => a.Applicant.BusinessNumber),
        new("OrganizationType", "Organization Type", "String", "Applicant.OrganizationType", a => a.Applicant.OrganizationType),
        new("Sector", "Sector", "String", "Applicant.Sector", a => a.Applicant.Sector),
        new("SubSector", "Sub-Sector", "String", "Applicant.SubSector", a => a.Applicant.SubSector),
        new("SectorSubSectorIndustryDesc", "Sub-Sector Industry Description", "String", "Applicant.SectorSubSectorIndustryDesc", a => a.Applicant.SectorSubSectorIndustryDesc),
        new("ApproxNumberOfEmployees", "Approximate Number Of Employees", "Number", "Applicant.ApproxNumberOfEmployees", a => a.Applicant.ApproxNumberOfEmployees),
        new("FiscalDay", "Fiscal Year End (FYE) Day", "Number", "Applicant.FiscalDay", a => a.Applicant.FiscalDay),
        new("FiscalMonth", "Fiscal Year End (FYE) Month", "String", "Applicant.FiscalMonth", a => a.Applicant.FiscalMonth),
    ];
}
