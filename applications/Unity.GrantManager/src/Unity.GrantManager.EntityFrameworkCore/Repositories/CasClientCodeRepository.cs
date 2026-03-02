using System;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Integrations;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ICasClientCodeRepository))]
    public class CasClientCodeRepository : EfCoreRepository<GrantManagerDbContext, CasClientCode, Guid>, ICasClientCodeRepository   
    {
        public CasClientCodeRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
