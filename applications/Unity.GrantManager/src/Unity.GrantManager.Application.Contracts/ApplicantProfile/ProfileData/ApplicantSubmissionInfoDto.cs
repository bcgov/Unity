using System.Collections.Generic;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class ApplicantSubmissionInfoDto : ApplicantProfileDataDto
    {
        public override string DataType => "SUBMISSIONINFO";

        public List<SubmissionInfoItemDto> Submissions { get; set; } = [];
        public string LinkSource { get; set; } = string.Empty;
    }
}
