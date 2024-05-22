using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
    [ExposeServices(typeof(IApplicantAddressRepository))]
    public class AddressRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : EfCoreRepository<GrantTenantDbContext, ApplicantAddress, Guid>(dbContextProvider), IApplicantAddressRepository
    {
        public async Task<List<ApplicantAddress>> FindByApplicantIdAsync(Guid applicantId)
        {
            var dbSet = await GetDbSetAsync();

            return await dbSet.Where(a => a.ApplicantId == applicantId).ToListAsync();
        }
    }
}
