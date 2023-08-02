using System;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IApplicationFormSubmissionRepository))]
    public class ApplicationFormSubmissionRepository : EfCoreRepository<GrantManagerDbContext, ApplicationFormSubmission, Guid>, IApplicationFormSubmissionRepository
    {
        public ApplicationFormSubmissionRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
