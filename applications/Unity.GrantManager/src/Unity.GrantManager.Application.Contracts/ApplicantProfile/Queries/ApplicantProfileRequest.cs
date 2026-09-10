using System;

namespace Unity.GrantManager.ApplicantProfile
{
    public class ApplicantProfileRequest
    {
        public Guid ProfileId { get; set; } = Guid.Empty;
        public string Subject { get; set; } = string.Empty;        
    }

    public class ApplicantProfileInfoRequest : ApplicantProfileRequest
    {
        public Guid TenantId { get; set; } = Guid.Empty;
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Identifies the submission whose form data is being requested. Required only when
        /// <see cref="Key"/> is <see cref="ApplicantProfileKeys.SubmissionFormData"/>.
        /// </summary>
        public Guid? SubmissionId { get; set; }
    }
}
