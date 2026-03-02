using System;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantTenantDto
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
    }
}
