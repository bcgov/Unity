using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantAddressInfoDto : ApplicantProfileDataDto
    {
        [JsonIgnore]
        public override string DataType => "ADDRESSINFO";

        public List<AddressInfoItemDto> Addresses { get; set; } = [];
    }
}
