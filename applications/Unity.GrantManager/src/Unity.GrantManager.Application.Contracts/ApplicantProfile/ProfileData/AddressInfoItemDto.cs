using System;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class AddressInfoItemDto
    {
        public Guid Id { get; set; }
        public string AddressType { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Street2 { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsEditable { get; set; }
        public string? ReferenceNo { get; set; }
    }
}
