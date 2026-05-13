using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantContactInfoDto : ApplicantProfileDataDto
    {
        [JsonIgnore]
        public override string DataType => "CONTACTINFO";
        
        public List<ContactInfoItemDto> Contacts { get; set; } = [];
    }
}
