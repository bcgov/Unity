using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applicants;

public interface IApplicantsService : IApplicationService
{
    Task<Applicant> CreateOrRetrieveApplicantAsync(IntakeMapping intakeMap);
    Task<ApplicantAgent> CreateOrUpdateApplicantAgentAsync(ApplicantAgentDto applicantAgentDto);
}
