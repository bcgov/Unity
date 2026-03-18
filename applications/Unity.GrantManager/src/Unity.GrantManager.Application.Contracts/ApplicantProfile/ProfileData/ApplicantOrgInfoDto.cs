using System.Collections.Generic;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantOrgInfoDto : ApplicantProfileDataDto
    {
        public override string DataType => "ORGINFO";

        public List<OrgInfoItemDto> Organizations { get; set; } = [];
    }
}
