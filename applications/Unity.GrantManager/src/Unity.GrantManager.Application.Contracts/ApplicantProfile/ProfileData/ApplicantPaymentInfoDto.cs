using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantPaymentInfoDto : ApplicantProfileDataDto
    {
        [JsonIgnore]
        public override string DataType => "PAYMENTINFO";

        public List<PaymentInfoItemDto> Payments { get; set; } = [];
    }
}
