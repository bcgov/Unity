using System;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ContactInfoItemDto
    {
        public Guid ContactId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Email { get; set; }
        public string? HomePhoneNumber { get; set; }
        public string? MobilePhoneNumber { get; set; }
        public string? WorkPhoneNumber { get; set; }
        public string? WorkPhoneExtension { get; set; }
        public string? ContactType { get; set; }
        public string? Role { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsEditable { get; set; }
        public Guid? ApplicationId { get; set; }
    }
}
