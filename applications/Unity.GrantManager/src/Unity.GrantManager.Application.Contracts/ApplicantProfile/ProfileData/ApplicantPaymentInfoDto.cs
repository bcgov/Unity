using System.Collections.Generic;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantPaymentInfoDto : ApplicantProfileDataDto
    {
        public override string DataType => "PAYMENTINFO";

        public List<PaymentInfoItemDto> Payments { get; set; } = [];
    }
}
