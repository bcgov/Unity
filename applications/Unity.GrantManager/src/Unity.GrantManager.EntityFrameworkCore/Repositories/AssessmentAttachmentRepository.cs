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

#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member. - ABP pattern issue, will not fix

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IAssessmentAttachmentRepository))]    
    public class AssessmentAttachmentRepository : EfCoreRepository<GrantTenantDbContext, AssessmentAttachment, Guid>, IAssessmentAttachmentRepository
    {
        public AssessmentAttachmentRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }
        public async Task<List<AssessmentAttachment>> GetListAsync(int skipCount, int maxResultCount, string sorting, string filter)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .WhereIf(
                    !filter.IsNullOrWhiteSpace(),
                    assessmentAttachment => 
                        assessmentAttachment.FileName != null 
                        && assessmentAttachment.FileName.Contains(filter)
                 )
                .OrderBy(sorting)
                .Skip(skipCount)
                .Take(maxResultCount)
                .ToListAsync();
        }
    }
}
