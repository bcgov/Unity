using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IFormMappingReviewRepository))]
public class FormMappingReviewRepository(
    IDbContextProvider<GrantTenantDbContext> dbContextProvider)
    : EfCoreRepository<GrantTenantDbContext, FormMappingReview, Guid>(dbContextProvider),
        IFormMappingReviewRepository
{
    public async Task<FormMappingReview?> FindByFormVersionAsync(Guid formVersionId)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.FormMappingReviews.FirstOrDefaultAsync(review => review.FormVersionId == formVersionId);
    }
}
