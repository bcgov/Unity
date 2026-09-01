using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Applications;

public sealed record ApplicantMergeValues
{
    public string? ApplicantName { get; init; }
    public string? UnityApplicantId { get; init; }
    public string? OrgName { get; init; }
    public string? OrgNumber { get; init; }
    public string? NonRegOrgName { get; init; }
    public string? OrganizationType { get; init; }
    public string? ApproxNumberOfEmployees { get; init; }
    public string? OrgStatus { get; init; }
    public string? IndigenousOrgInd { get; init; }
    public string? Sector { get; init; }
    public string? SubSector { get; init; }
    public string? SectorSubSectorIndustryDesc { get; init; }
    public int? FiscalDay { get; init; }
    public string? FiscalMonth { get; init; }

    public bool IsComposedFrom(Applicant principal, Applicant secondary)
    {
        return IsOneOf(ApplicantName, principal.ApplicantName, secondary.ApplicantName)
            && IsOneOf(UnityApplicantId, principal.UnityApplicantId, secondary.UnityApplicantId)
            && IsOneOf(OrgName, principal.OrgName, secondary.OrgName)
            && IsOneOf(OrgNumber, principal.OrgNumber, secondary.OrgNumber)
            && IsOneOf(NonRegOrgName, principal.NonRegOrgName, secondary.NonRegOrgName)
            && IsOneOf(OrganizationType, principal.OrganizationType, secondary.OrganizationType)
            && IsOneOf(ApproxNumberOfEmployees, principal.ApproxNumberOfEmployees, secondary.ApproxNumberOfEmployees)
            && IsOneOf(OrgStatus, principal.OrgStatus, secondary.OrgStatus)
            && IsOneOf(IndigenousOrgInd, principal.IndigenousOrgInd, secondary.IndigenousOrgInd)
            && IsOneOf(Sector, principal.Sector, secondary.Sector)
            && IsOneOf(SubSector, principal.SubSector, secondary.SubSector)
            && IsOneOf(SectorSubSectorIndustryDesc, principal.SectorSubSectorIndustryDesc, secondary.SectorSubSectorIndustryDesc)
            && IsOneOf(FiscalDay, principal.FiscalDay, secondary.FiscalDay)
            && IsOneOf(FiscalMonth, principal.FiscalMonth, secondary.FiscalMonth);
    }

    public void ApplyTo(Applicant applicant)
    {
        applicant.ApplicantName = ApplicantName;
        applicant.UnityApplicantId = UnityApplicantId;
        applicant.OrgName = OrgName;
        applicant.OrgNumber = OrgNumber;
        applicant.NonRegOrgName = NonRegOrgName;
        applicant.OrganizationType = OrganizationType;
        applicant.ApproxNumberOfEmployees = ApproxNumberOfEmployees;
        applicant.OrgStatus = OrgStatus;
        applicant.IndigenousOrgInd = IndigenousOrgInd;
        applicant.Sector = Sector;
        applicant.SubSector = SubSector;
        applicant.SectorSubSectorIndustryDesc = SectorSubSectorIndustryDesc;
        applicant.FiscalDay = FiscalDay;
        applicant.FiscalMonth = FiscalMonth;
    }

    private static bool IsOneOf<T>(T value, T first, T second)
    {
        if (typeof(T) == typeof(string))
        {
            var normalizedValue = (string?)(object?)value ?? string.Empty;
            return string.Equals(normalizedValue, (string?)(object?)first ?? string.Empty, StringComparison.Ordinal)
                || string.Equals(normalizedValue, (string?)(object?)second ?? string.Empty, StringComparison.Ordinal);
        }

        return EqualityComparer<T>.Default.Equals(value, first)
            || EqualityComparer<T>.Default.Equals(value, second);
    }
}

public sealed record ApplicantMergeApplicantSnapshot
{
    public string? ApplicantName { get; init; }
    public string? UnityApplicantId { get; init; }
    public string? OrgName { get; init; }
    public string? OrgNumber { get; init; }
    public string? NonRegOrgName { get; init; }
    public string? OrganizationType { get; init; }
    public string? ApproxNumberOfEmployees { get; init; }
    public string? OrgStatus { get; init; }
    public string? IndigenousOrgInd { get; init; }
    public string? Sector { get; init; }
    public string? SubSector { get; init; }
    public string? SectorSubSectorIndustryDesc { get; init; }
    public int? FiscalDay { get; init; }
    public string? FiscalMonth { get; init; }
    public Guid? SupplierId { get; init; }
    public bool IsDuplicated { get; init; }

    public static ApplicantMergeApplicantSnapshot FromApplicant(Applicant applicant)
    {
        return new ApplicantMergeApplicantSnapshot
        {
            ApplicantName = applicant.ApplicantName,
            UnityApplicantId = applicant.UnityApplicantId,
            OrgName = applicant.OrgName,
            OrgNumber = applicant.OrgNumber,
            NonRegOrgName = applicant.NonRegOrgName,
            OrganizationType = applicant.OrganizationType,
            ApproxNumberOfEmployees = applicant.ApproxNumberOfEmployees,
            OrgStatus = applicant.OrgStatus,
            IndigenousOrgInd = applicant.IndigenousOrgInd,
            Sector = applicant.Sector,
            SubSector = applicant.SubSector,
            SectorSubSectorIndustryDesc = applicant.SectorSubSectorIndustryDesc,
            FiscalDay = applicant.FiscalDay,
            FiscalMonth = applicant.FiscalMonth,
            SupplierId = applicant.SupplierId,
            IsDuplicated = applicant.IsDuplicated
        };
    }

    public void Restore(Applicant applicant)
    {
        new ApplicantMergeValues
        {
            ApplicantName = ApplicantName,
            UnityApplicantId = UnityApplicantId,
            OrgName = OrgName,
            OrgNumber = OrgNumber,
            NonRegOrgName = NonRegOrgName,
            OrganizationType = OrganizationType,
            ApproxNumberOfEmployees = ApproxNumberOfEmployees,
            OrgStatus = OrgStatus,
            IndigenousOrgInd = IndigenousOrgInd,
            Sector = Sector,
            SubSector = SubSector,
            SectorSubSectorIndustryDesc = SectorSubSectorIndustryDesc,
            FiscalDay = FiscalDay,
            FiscalMonth = FiscalMonth
        }.ApplyTo(applicant);

        applicant.SupplierId = SupplierId;
        applicant.IsDuplicated = IsDuplicated;
    }
}

public sealed record ApplicantMergeRelatedRecordsSnapshot
{
    public List<Guid> ApplicationFormSubmissionIds { get; init; } = [];
    public List<Guid> ApplicantAgentIds { get; init; } = [];
    public List<Guid> ApplicantAddressIds { get; init; } = [];
}

public sealed record ApplicantMergeReversibility(
    ApplicantMergeOperation Operation,
    bool CanReverse,
    string? ErrorCode);
