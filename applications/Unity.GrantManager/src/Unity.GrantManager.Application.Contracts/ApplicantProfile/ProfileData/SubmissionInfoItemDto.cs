using System;

namespace Unity.GrantManager.ApplicantProfile.ProfileData
{
    public class SubmissionInfoItemDto
    {
        public Guid Id { get; set; }
        public string LinkId { get; set; } = string.Empty;
        public DateTime ReceivedTime { get; set; }
        public DateTime SubmissionTime { get; set; }
        public string ReferenceNo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
