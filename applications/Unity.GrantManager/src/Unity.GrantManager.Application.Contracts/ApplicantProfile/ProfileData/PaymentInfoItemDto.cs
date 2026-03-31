using System;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class PaymentInfoItemDto
    {
        public Guid Id { get; set; }
        public string PaymentNumber { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? PaymentDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
