using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IAssessmentsRepository))]
    public class AssessmentsRepository : EfCoreRepository<GrantManagerDbContext, Assessment, Guid>, IAssessmentsRepository
    {
        public AssessmentsRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {

        }

        public async Task<bool> ExistsAsync(Guid applicationId, Guid userId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.AnyAsync(x =>
                x.ApplicationId == applicationId && x.AssignedUserId == userId);
        }
    }
}