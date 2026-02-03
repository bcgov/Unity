using System;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantProfileRequest
    {
        public Guid ProfileId { get; set; } = Guid.NewGuid();
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;        
    }
}
