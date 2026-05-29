using System;
using Unity.GrantManager.ApplicantProfile.ProfileData;

namespace Unity.GrantManager.ApplicantProfile
{
    public class ApplicantProfileDto
    {
        public Guid ProfileId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public ApplicantProfileDataDto? Data { get; set; }
    }
}
