using System;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IAdjudicationAttachmentRepository))]
    public class AdjudicationAttachmentRepository : EfCoreRepository<GrantManagerDbContext, AdjudicationAttachment, Guid>, IAdjudicationAttachmentRepository
    {
        public AdjudicationAttachmentRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
    }
}
