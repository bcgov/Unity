using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager;

public class GrantManagerDataSeederContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IApplicationStatusRepository _applicationStatusRepository;

    public GrantManagerDataSeederContributor(IApplicationStatusRepository applicationStatusRepository)
    {
        _applicationStatusRepository = applicationStatusRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId != null) // only try seed into a tenant database
        {
            ApplicationStatus? status1 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.SUBMITTED);
            status1 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.SUBMITTED,
                    ExternalStatus = "Submitted",
                    InternalStatus = "Submitted"
                }
            );

            ApplicationStatus? status2 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.ASSIGNED);
            status2 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.ASSIGNED,
                    ExternalStatus = "Under Review",
                    InternalStatus = "Assigned"
                }
            );

            ApplicationStatus? status3 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.WITHDRAWN);
            status3 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.WITHDRAWN,
                    ExternalStatus = "Withdrawn",
                    InternalStatus = "Withdrawn"
                }
            );

            ApplicationStatus? status4 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.CLOSED);
            status4 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.CLOSED,
                    ExternalStatus = "Closed",
                    InternalStatus = "Closed"
                }
            );

            ApplicationStatus? status5 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.UNDER_INITIAL_REVIEW);
            status5 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.UNDER_INITIAL_REVIEW,
                    ExternalStatus = "Under Review",
                    InternalStatus = "Under Initial Review"
                }
            );

            ApplicationStatus? status6 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.INITITAL_REVIEW_COMPLETED);
            status6 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.INITITAL_REVIEW_COMPLETED,
                    ExternalStatus = "Under Review",
                    InternalStatus = "Initial Review Completed"
                }
            );

            ApplicationStatus? status7 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.UNDER_ASSESSMENT);
            status7 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.UNDER_ASSESSMENT,
                    ExternalStatus = "Under Review",
                    InternalStatus = "Under Assessment"
                }
            );

            ApplicationStatus? status8 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.ASSESSMENT_COMPLETED);
            status8 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.ASSESSMENT_COMPLETED,
                    ExternalStatus = "Under Review",
                    InternalStatus = "Assessment Completed"
                }
            );

            ApplicationStatus? status9 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.GRANT_APPROVED);
            status9 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.GRANT_APPROVED,
                    ExternalStatus = "Grant Approved",
                    InternalStatus = "Grant Approved"
                }
            );

            ApplicationStatus? status10 = await _applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == GrantApplicationState.GRANT_NOT_APPROVED);
            status10 ??= await _applicationStatusRepository.InsertAsync(
                new ApplicationStatus
                {
                    StatusCode = GrantApplicationState.GRANT_NOT_APPROVED,
                    ExternalStatus = "Declined",
                    InternalStatus = "Declined"
                }
            );
        }
    }
}
