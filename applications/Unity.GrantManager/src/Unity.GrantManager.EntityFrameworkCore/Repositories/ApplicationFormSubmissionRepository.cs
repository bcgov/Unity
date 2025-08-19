using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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

        public async Task<ApplicationFormDetails> GetFormDetailsByApplicationIdAsync(Guid applicationId)
        {
            var dbContext = await GetDbContextAsync();
            
            // Join ApplicationFormSubmission with ApplicationForm and ApplicationFormVersion 
            // to get all required data in a single query
            return await dbContext.ApplicationFormSubmissions
                .Where(s => s.ApplicationId == applicationId)
                .Join(
                    dbContext.ApplicationFormVersions,
                    submission => submission.FormVersionId,
                    version => version.Id,
                    (submission, version) => new { submission, version }
                )
                .Join(
                    dbContext.ApplicationForms,
                    formVersion => formVersion.version.ApplicationFormId,
                    form => form.Id,
                    (formVersion, form) => new ApplicationFormDetails
                    {
                        ApplicationId = applicationId,
                        ApplicationFormId = form.Id,
                        ApplicationFormName = form.ApplicationFormName ?? string.Empty,
                        ApplicationFormDescription = form.ApplicationFormDescription ?? string.Empty,
                        ApplicationFormCategory = form.Category ?? string.Empty,
                        ApplicationFormVersionId = formVersion.version.Id,
                        ApplicationFormVersion = formVersion.version.Version ?? 0
                    }
                )
                .FirstOrDefaultAsync() ?? new ApplicationFormDetails { ApplicationId = applicationId };
        }
    }
}
