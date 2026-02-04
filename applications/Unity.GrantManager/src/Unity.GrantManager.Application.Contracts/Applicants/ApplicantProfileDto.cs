using System;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantProfileDto
    {
        public Guid ProfileId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
