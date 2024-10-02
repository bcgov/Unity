using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IAssessmentRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
// Will not fix this, as is a ABP implementation issue
public class AssessmentRepository : EfCoreRepository<GrantTenantDbContext, Assessment, Guid>, IAssessmentRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
{
    public AssessmentRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<bool> ExistsAsync(Guid applicationId, Guid userId)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet.AnyAsync(x =>
            x.ApplicationId == applicationId && x.AssessorId == userId);
    }

    public async Task<List<Assessment>> GetListByApplicationId(Guid applicationId)
    {
        var dbSet = await GetDbSetAsync();
        return await dbSet.Where(x => x.ApplicationId == applicationId).ToListAsync();
    }

    public async Task<List<AssessmentWithAssessorQueryResultItem>> GetListWithAssessorsAsync(Guid applicationId)
    {
        var assessmentQueryable = await GetQueryableAsync();
        var userQueryable = (await GetDbContextAsync()).Set<Person>().AsQueryable();

        var query = assessmentQueryable
            .Where(x => x.ApplicationId == applicationId)
            .Join(
                userQueryable,
                assessment => assessment.AssessorId,
                user => user.Id,
                (assessment, user) => new AssessmentWithAssessorQueryResultItem
                {
                    Id = assessment.Id,
                    ApplicationId = assessment.ApplicationId,

                    AssessorId = assessment.AssessorId,
                    AssessorDisplayName = user.OidcDisplayName,
                    AssessorFullName = user.FullName,
                    AssessorBadge = user.Badge,

                    StartDate = assessment.CreationTime,
                    EndDate = assessment.EndDate,
                    Status = assessment.Status,
                    IsComplete = assessment.IsComplete,
                    ApprovalRecommended = assessment.ApprovalRecommended,
                    FinancialAnalysis = assessment.FinancialAnalysis,
                    EconomicImpact = assessment.EconomicImpact,
                    InclusiveGrowth = assessment.InclusiveGrowth,
                    CleanGrowth = assessment.CleanGrowth
                });

        return await query.ToListAsync();
    }
}