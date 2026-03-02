using System;

namespace Unity.GrantManager.ApplicantProfile
{
    public class ApplicantProfileRequest
    {
        public Guid ProfileId { get; set; } = Guid.Empty;
        public string Subject { get; set; } = string.Empty;        
    }

    public class ApplicantProfileInfoRequest : ApplicantProfileRequest
    {
        public Guid TenantId { get; set; } = Guid.Empty;
        public string Key { get; set; } = string.Empty;
    }
}
