using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantTenantDto
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = [];
    }
}
