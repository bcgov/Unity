using Newtonsoft.Json;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantContactInfoDto : ApplicantProfileDataDto
    {
        public override string DataType => "CONTACTINFO";

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public List<ContactInfoItemDto> Contacts { get; set; } = [];
    }
}
