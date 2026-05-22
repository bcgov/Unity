using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantSubmissionInfoDto : ApplicantProfileDataDto
    {
        [JsonIgnore]
        public override string DataType => "SUBMISSIONINFO";

        public List<SubmissionInfoItemDto> Submissions { get; set; } = [];
        public string LinkSource { get; set; } = string.Empty;
    }
}
