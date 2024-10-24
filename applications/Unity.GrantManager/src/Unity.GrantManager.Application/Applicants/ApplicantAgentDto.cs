using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;

namespace Unity.GrantManager.Applicants
{
    public class ApplicantAgentDto
    {
        public required IntakeMapping IntakeMap { get; set; }
        public required Applicant Applicant { get; set; }
        public required Application Application { get; set; }
    }
}