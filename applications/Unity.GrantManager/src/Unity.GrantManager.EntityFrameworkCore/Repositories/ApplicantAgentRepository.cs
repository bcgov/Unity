﻿using System;
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
    [ExposeServices(typeof(IApplicantAgentRepository))]
    #pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    public class ApplicantAgentRepository : EfCoreRepository<GrantTenantDbContext, ApplicantAgent, Guid>, IApplicantAgentRepository
    #pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
    {
        public ApplicantAgentRepository(IDbContextProvider<GrantTenantDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<ApplicantAgent?> GetByApplicantIdAsync(Guid applicantId)
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.ApplicantAgents.FirstOrDefaultAsync(x => x.ApplicantId == applicantId);
        }

    }
}