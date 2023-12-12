using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;


namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IApplicationChefsFileAttachmentRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    // This pattern is an implementation ontop of ABP framework, will not change this
    public class ApplicationChefsFileAttachmentRepository : EfCoreRepository<GrantTenantDbContext, ApplicationChefsFileAttachment, Guid>, IApplicationChefsFileAttachmentRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    {
        public ApplicationChefsFileAttachmentRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
        public async Task<List<ApplicationChefsFileAttachment>> GetListAsync(Guid applicationId)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync();
        }
    }
}
