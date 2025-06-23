using System;

namespace Unity.GrantManager.Applicants
{
    public class SetApplicantDuplicateDto
    {
        public Guid PrincipalApplicantId { get; set; }
        public Guid NonPrincipalApplicantId { get; set; }
    }
}
