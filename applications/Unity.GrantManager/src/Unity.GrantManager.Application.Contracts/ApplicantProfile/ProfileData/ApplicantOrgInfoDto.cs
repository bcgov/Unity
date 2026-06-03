using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantOrgInfoDto : ApplicantProfileDataDto
    {
        [JsonIgnore]
        public override string DataType => "ORGINFO";

        public List<OrgInfoItemDto> Organizations { get; set; } = [];
    }
}
