using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Repositories;

[ExposeServices(typeof(IGenerationReviewRepository))]
public class GenerationReviewRepository(
    IDbContextProvider<GrantTenantDbContext> dbContextProvider)
    : EfCoreRepository<GrantTenantDbContext, GenerationReview, Guid>(dbContextProvider),
        IGenerationReviewRepository
{
    public async Task<GenerationReview?> FindLatestByOperationAndFormVersionAsync(
        string operation,
        Guid formVersionId)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.GenerationReviews
            .Where(review =>
                review.Operation == operation &&
                review.ContextId == formVersionId)
            .OrderByDescending(review => review.Sequence)
            .FirstOrDefaultAsync();
    }

    public async Task<List<GenerationReview>> GetListByOperationAndFormVersionAsync(
        string operation,
        Guid formVersionId)
    {
        var dbContext = await GetDbContextAsync();
        return await dbContext.GenerationReviews
            .Where(review =>
                review.Operation == operation &&
                review.ContextId == formVersionId)
            .OrderBy(review => review.Sequence)
            .ToListAsync();
    }
}
