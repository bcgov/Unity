using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager;

public class GrantManagerDataSeederContributor(
    IApplicationStatusRepository applicationStatusRepository) : IDataSeedContributor, ITransientDependency
{
    public static class GrantApplicationStates
    {
        public const string SUBMITTED = "Submitted";
        public const string ASSIGNED = "Assigned";
        public const string WITHDRAWN = "Withdrawn";
        public const string CLOSED = "Closed";
        public const string UNDER_REVIEW = "Under Review";
        public const string UNDER_INITIAL_REVIEW = "Under Initial Review";
        public const string INITITAL_REVIEW_COMPLETED = "Initial Review Completed";
        public const string UNDER_ASSESSMENT = "Under Assessment";
        public const string ASSESSMENT_COMPLETED = "Assessment Completed";
        public const string GRANT_APPROVED = "Grant Approved";
        public const string DECLINED = "Declined";
        public const string DEFER = "Deferred";
        public const string ON_HOLD = "On Hold";
    }

    public async Task SeedAsync(DataSeedContext context)
    {

        if (context.TenantId == null) // only seed into a tenant database
        {
           return;
        }   

        await SeedApplicationStatusAsync();
    }

    
    private async Task SeedApplicationStatusAsync()
    {
        var statuses = new List<ApplicationStatus>
        {
            new() { StatusCode = GrantApplicationState.SUBMITTED, ExternalStatus = GrantApplicationStates.SUBMITTED, InternalStatus = GrantApplicationStates.SUBMITTED },
            new() { StatusCode = GrantApplicationState.ASSIGNED, ExternalStatus = GrantApplicationStates.UNDER_REVIEW, InternalStatus = GrantApplicationStates.ASSIGNED },
            new() { StatusCode = GrantApplicationState.WITHDRAWN, ExternalStatus = GrantApplicationStates.WITHDRAWN, InternalStatus = GrantApplicationStates.WITHDRAWN },
            new() { StatusCode = GrantApplicationState.CLOSED, ExternalStatus = GrantApplicationStates.CLOSED, InternalStatus = GrantApplicationStates.CLOSED },
            new() { StatusCode = GrantApplicationState.UNDER_INITIAL_REVIEW, ExternalStatus = GrantApplicationStates.UNDER_REVIEW, InternalStatus = GrantApplicationStates.UNDER_INITIAL_REVIEW },
            new() { StatusCode = GrantApplicationState.INITITAL_REVIEW_COMPLETED, ExternalStatus = GrantApplicationStates.UNDER_REVIEW, InternalStatus = GrantApplicationStates.INITITAL_REVIEW_COMPLETED },
            new() { StatusCode = GrantApplicationState.UNDER_ASSESSMENT, ExternalStatus = GrantApplicationStates.UNDER_REVIEW, InternalStatus = GrantApplicationStates.UNDER_ASSESSMENT },
            new() { StatusCode = GrantApplicationState.ASSESSMENT_COMPLETED, ExternalStatus = GrantApplicationStates.UNDER_REVIEW, InternalStatus = GrantApplicationStates.ASSESSMENT_COMPLETED },
            new() { StatusCode = GrantApplicationState.GRANT_APPROVED, ExternalStatus = GrantApplicationStates.GRANT_APPROVED, InternalStatus = GrantApplicationStates.GRANT_APPROVED },
            new() { StatusCode = GrantApplicationState.GRANT_NOT_APPROVED, ExternalStatus = GrantApplicationStates.DECLINED, InternalStatus = GrantApplicationStates.DECLINED },
            new() { StatusCode = GrantApplicationState.DEFER, ExternalStatus = GrantApplicationStates.DEFER, InternalStatus = GrantApplicationStates.DEFER },
            new() { StatusCode = GrantApplicationState.ON_HOLD, ExternalStatus = GrantApplicationStates.ON_HOLD, InternalStatus = GrantApplicationStates.ON_HOLD },
        };

        foreach (var status in statuses)
        {
            var existing = await applicationStatusRepository.FirstOrDefaultAsync(s => s.StatusCode == status.StatusCode);
            if (existing == null)
            {
                await applicationStatusRepository.InsertAsync(status);
            }
        }
    }
}
