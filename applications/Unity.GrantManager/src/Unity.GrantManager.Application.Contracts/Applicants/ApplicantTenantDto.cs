using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantTenantDto
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? DefaultFromAddress { get; set; }
    }
}
