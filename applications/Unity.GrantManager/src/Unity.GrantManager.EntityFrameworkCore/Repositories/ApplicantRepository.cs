using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.Applications;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.Payments.Domain.Suppliers;
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
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
            // EF Core cannot translate StringComparison overloads to SQL, so we use ToLower() for database compatibility
            return await dbContext.Applicants
                .FirstOrDefaultAsync(a => a.ApplicantName != null &&
                                          a.ApplicantName.ToLower() == unityApplicantNameNormalized);
#pragma warning restore CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons

        }
        public async Task<List<Applicant>> GetApplicantsWithUnityApplicantIdAsync()
        {
            var dbContext = await GetDbContextAsync();
            return await dbContext.Applicants
                .Where(x => x.UnityApplicantId != null)
                .ToListAsync();
        }

        public async Task<JsonDocument> GetApplicantAutocompleteQueryAsync(string? applicantLookUpQuery)
        {
            if (string.IsNullOrWhiteSpace(applicantLookUpQuery))
            {
                // Return an empty JSON array if the query is null or only whitespace
                return JsonDocument.Parse("[]");
            }

            var dbContext = await GetDbContextAsync();
            var searchQuery = applicantLookUpQuery.ToLower();

            var applicants = await dbContext.Applicants
            .AsNoTracking()
            .ToListAsync();

            var filtered = applicants
                .Where(a =>
                    (!string.IsNullOrEmpty(a.ApplicantName) && a.ApplicantName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(a.UnityApplicantId) && a.UnityApplicantId.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                )
                .Take(10)
                .ToList();

            // Batch-fetch supplier data for the matched applicants
            var supplierIds = filtered
                .Where(a => a.SupplierId.HasValue)
                .Select(a => a.SupplierId!.Value)
                .Distinct()
                .ToList();

            Dictionary<Guid, (string? Number, string? Name, string? Status)> supplierMap = new();
            if (supplierIds.Count > 0)
            {
                var suppliers = await dbContext.Set<Supplier>()
                    .AsNoTracking()
                    .Where(s => supplierIds.Contains(s.Id) && !s.IsDeleted)
                    .Select(s => new { s.Id, s.Number, s.Name, s.Status })
                    .ToListAsync();
                supplierMap = suppliers.ToDictionary(s => s.Id, s => (s.Number, s.Name, s.Status));
            }

            var filteredApplicants = filtered.Select(a =>
            {
                supplierMap.TryGetValue(a.SupplierId ?? Guid.Empty, out var sup);
                return new
                {
                    a.Id,
                    a.ApplicantName,
                    a.OrgName,
                    a.OrgNumber,
                    a.NonRegOrgName,
                    a.OrganizationType,
                    a.OrganizationSize,
                    a.ApproxNumberOfEmployees,
                    a.OrgStatus,
                    a.BusinessNumber,
                    a.IndigenousOrgInd,
                    a.Sector,
                    a.SubSector,
                    a.SectorSubSectorIndustryDesc,
                    a.FiscalDay,
                    a.FiscalMonth,
                    a.UnityApplicantId,
                    a.IsDuplicated,
                    SupplierId = a.SupplierId?.ToString(),
                    SupplierNumber = sup.Number,
                    SupplierName = sup.Name,
                    SupplierStatus = sup.Status
                };
            });

            var json = JsonSerializer.Serialize(filteredApplicants);
            return JsonDocument.Parse(json);
        }
    }
}
