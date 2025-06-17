using System;

namespace Unity.GrantManager.Applicants
{
    public class UpdateApplicantIdDto
    {
        public Guid ApplicationId { get; set; }
        public Guid ApplicantId { get; set; }
    }
}
