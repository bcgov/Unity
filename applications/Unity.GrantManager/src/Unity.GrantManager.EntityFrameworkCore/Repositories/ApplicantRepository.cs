using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IApplicantRepository))]
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    // This pattern is an implementation ontop of ABP framework, will not change this
    public class ApplicantRepository : EfCoreRepository<GrantTenantDbContext, Applicant, Guid>, IApplicantRepository
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    {
        public ApplicantRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<List<Applicant>> GetUnmatchedApplicantsAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Applicants
                .Where(x => x.MatchPercentage == null)
                .ToListAsync();
        }

        public async Task<Applicant?> GetByUnityApplicantIdAsync(string unityApplicantId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Applicants.FirstOrDefaultAsync(x => x.UnityApplicantId == unityApplicantId);
        }

        public async Task<Applicant?> GetByUnityApplicantNameAsync(string unityApplicantName)
        {
            string unityApplicantNameNormalized = unityApplicantName.Trim().ToLower();  // Normalize the input

            var dbContext = await GetDbContextAsync();
            return await dbContext.Applicants
                .FirstOrDefaultAsync(a => a.ApplicantName != null &&
                                          a.ApplicantName.ToLower() == unityApplicantNameNormalized);

        }
        public async Task<List<Applicant>> GetApplicantsWithUnityApplicantIdAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Applicants
                .Where(x => x.UnityApplicantId != null)
                .ToListAsync();
        }

        public async Task<List<Applicant>> GetApplicantsBySiteIdAsync(Guid siteId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Applicants
                .Where(x => x.SiteId == siteId)
                .ToListAsync();
        }
    }
}
