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
        public string? OrganizationSize { get; set; }
        public string? Sector { get; set; }
        public string? SubSector { get; set; }
    }
}
