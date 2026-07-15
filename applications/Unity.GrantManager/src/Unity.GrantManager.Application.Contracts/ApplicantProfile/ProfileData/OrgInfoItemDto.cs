using System;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class OrgInfoItemDto
    {
        public Guid Id { get; set; }
        public string? ApplicantRefId { get; set; }
        public string? ApplicantName { get; set; }
        public string? OrgName { get; set; }
        public string? OrganizationType { get; set; }
        public string? OrgNumber { get; set; }
        public string? OrgStatus { get; set; }
        public string? NonRegOrgName { get; set; }
        public string? FiscalMonth { get; set; }
        public int? FiscalDay { get; set; }
        // Kept for Grants Portal backward compatibility — the portal reads this JSON key.
        // Value is sourced from Applicant.ApproxNumberOfEmployees, not Applicant.OrganizationSize.
        // Portal developer can migrate to ApproxNumberOfEmployees once this field is confirmed stable.
        public string? OrganizationSize { get; set; }
        public string? ApproxNumberOfEmployees { get; set; }
        public string? Sector { get; set; }
        public string? SubSector { get; set; }
    }
}
