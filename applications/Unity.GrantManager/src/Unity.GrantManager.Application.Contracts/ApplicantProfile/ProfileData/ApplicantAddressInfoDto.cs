using System.Collections.Generic;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantAddressInfoDto : ApplicantProfileDataDto
    {
        public override string DataType => "ADDRESSINFO";

        public List<AddressInfoItemDto> Addresses { get; set; } = [];
    }
}
