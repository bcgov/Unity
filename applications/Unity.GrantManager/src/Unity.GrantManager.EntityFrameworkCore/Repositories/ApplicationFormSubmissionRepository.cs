using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IApplicationFormSubmissionRepository))]
    public class ApplicationFormSubmissionRepository : EfCoreRepository<GrantTenantDbContext, ApplicationFormSubmission, Guid>, IApplicationFormSubmissionRepository
    {
        public ApplicationFormSubmissionRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<ApplicationFormSubmission> GetByApplicationAsync(Guid applicationId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.ApplicationFormSubmissions
                .FirstAsync(s => s.ApplicationId == applicationId);
        }
    }
}
