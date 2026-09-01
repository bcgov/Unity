using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IApplicantMergeOperationRepository))]
public class ApplicantMergeOperationRepository(
    IDbContextProvider<GrantTenantDbContext> dbContextProvider)
    : EfCoreRepository<GrantTenantDbContext, ApplicantMergeOperation, Guid>(dbContextProvider),
        IApplicantMergeOperationRepository
{
    public async Task<ApplicantMergeOperation> GetWithChangesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Include(item => item.ApplicationChanges)
            .FirstOrDefaultAsync(item => item.Id == id, GetCancellationToken(cancellationToken))
            ?? throw new EntityNotFoundException(typeof(ApplicantMergeOperation), id);
    }

    public async Task<List<ApplicantMergeOperation>> GetActiveForApplicantAsync(
        Guid applicantId,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet
            .Include(item => item.ApplicationChanges)
            .Where(item => item.Status == ApplicantMergeStatus.Completed
                && (item.PrincipalApplicantId == applicantId || item.SecondaryApplicantId == applicantId))
            .OrderByDescending(item => item.MergedAt)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<bool> HasLaterActiveMergeAsync(
        ApplicantMergeOperation operation,
        CancellationToken cancellationToken = default)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet.AnyAsync(item =>
                item.Id != operation.Id
                && item.Status == ApplicantMergeStatus.Completed
                && item.MergedAt > operation.MergedAt
                && (item.PrincipalApplicantId == operation.PrincipalApplicantId
                    || item.SecondaryApplicantId == operation.PrincipalApplicantId
                    || item.PrincipalApplicantId == operation.SecondaryApplicantId
                    || item.SecondaryApplicantId == operation.SecondaryApplicantId),
            GetCancellationToken(cancellationToken));
    }
}
