using System;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IChefsMissedSubmissionsRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    // This pattern is an implementation ontop of ABP framework, will not change this
    public class ChefsMissedSubmissionsRepository : EfCoreRepository<GrantManagerDbContext, Intakes.ChefsMissedSubmission, Guid>, IChefsMissedSubmissionsRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    {
        public ChefsMissedSubmissionsRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
