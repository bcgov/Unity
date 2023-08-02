using System;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.GrantPrograms;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IIntakeRepository))]
    public class IntakeRepository : EfCoreRepository<GrantManagerDbContext, Intake, Guid>, IIntakeRepository
    {
        public IntakeRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
