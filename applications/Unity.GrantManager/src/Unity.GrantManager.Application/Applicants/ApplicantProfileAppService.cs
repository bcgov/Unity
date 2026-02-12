using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Applicants
{
    [RemoteService(false)]
    public class ApplicantProfileAppService(
            ICurrentTenant currentTenant,
            ITenantRepository tenantRepository,
            IRepository<ApplicantTenantMap, Guid> applicantTenantMapRepository,
            IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository)
        : ApplicationService, IApplicantProfileAppService
    {

        /// <summary>
        /// Retrieves the applicant's profile information based on the specified request.
        /// </summary>
        /// <param name="request">An object containing the criteria used to identify the applicant profile to retrieve. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see
        /// cref="ApplicantProfileDto"/> with the applicant's profile data.</returns>
        public async Task<ApplicantProfileDto> GetApplicantProfileAsync(ApplicantProfileRequest request)
        {
            return await Task.FromResult(new ApplicantProfileDto
            {
                ProfileId = request.ProfileId,
                Subject = request.Subject,                
                Email = string.Empty,
                DisplayName = string.Empty
            });
        }

        /// <summary>
        /// Retrieves a list of tenants associated with the specified applicant profile.
        /// </summary>
        /// <remarks>The method extracts the username portion from the subject identifier in the request
        /// to match tenant mappings. This operation is asynchronous and queries the host database for relevant tenant
        /// associations.</remarks>
        /// <param name="request">An object containing applicant profile information, including the subject identifier used to locate tenant
        /// mappings.</param>
        /// <returns>A list of <see cref="ApplicantTenantDto"/> objects representing the tenants linked to the applicant. The
        /// list will be empty if no tenant associations are found.</returns>
        public async Task<List<ApplicantTenantDto>> GetApplicantTenantsAsync(ApplicantProfileRequest request)
        {
            // Extract the username part from the OIDC sub (part before '@')
            var subUsername = request.Subject.Contains('@')
                ? request.Subject[..request.Subject.IndexOf('@')].ToUpperInvariant()
                : request.Subject.ToUpperInvariant();

            // Query the ApplicantTenantMaps table in the host database
            using (currentTenant.Change(null))
            {
                var queryable = await applicantTenantMapRepository.GetQueryableAsync();
                var mappings = await queryable
                    .Where(m => m.OidcSubUsername == subUsername)
                    .Select(m => new ApplicantTenantDto
                    {
                        TenantId = m.TenantId,
                        TenantName = m.TenantName
                    })
                    .ToListAsync();

                return mappings;
            }
        }

        /// <summary>
        /// Reconciles ApplicantTenantMaps by scanning all tenants for submissions
        /// and ensuring mappings exist in the host database.
        /// Phase 1: Collects all distinct OidcSub-to-tenant associations into memory.
        /// Phase 2: Switches to host DB once and reconciles all mappings.
        /// </summary>
        /// <returns>Tuple of (created count, updated count)</returns>
        public async Task<(int Created, int Updated)> ReconcileApplicantTenantMapsAsync()
        {
            Logger.LogInformation("Starting ApplicantTenantMap reconciliation...");

            // Phase 1: Collect all OidcSub-to-tenant associations from each tenant DB
            var desiredMappings = new List<(string SubUsername, Guid TenantId, string TenantName)>();
            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                try
                {
                    Logger.LogDebug("Collecting submissions from tenant: {TenantName}", tenant.Name);

                    using (currentTenant.Change(tenant.Id))
                    {
                        var submissionQueryable = await applicationFormSubmissionRepository.GetQueryableAsync();
                        var distinctOidcSubs = await submissionQueryable
                            .Where(s => !string.IsNullOrWhiteSpace(s.OidcSub) && s.OidcSub != Guid.Empty.ToString())
                            .Select(s => s.OidcSub)
                            .Distinct()
                            .ToListAsync();

                        foreach (var oidcSub in distinctOidcSubs)
                        {
                            var subUsername = oidcSub.Contains('@')
                                ? oidcSub[..oidcSub.IndexOf('@')].ToUpperInvariant()
                                : oidcSub.ToUpperInvariant();

                            desiredMappings.Add((subUsername, tenant.Id, tenant.Name));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error collecting submissions for tenant {TenantName}", tenant.Name);
                }
            }

            if (desiredMappings.Count == 0)
            {
                Logger.LogInformation("ApplicantTenantMap reconciliation completed. No submissions found across tenants.");
                return (0, 0);
            }

            // Phase 2: Switch to host DB once, load existing mappings, and reconcile
            int totalMappingsCreated = 0;
            int totalMappingsUpdated = 0;

            using (currentTenant.Change(null))
            {
                var allSubUsernames = desiredMappings.Select(m => m.SubUsername).Distinct().ToList();

                var mapQueryable = await applicantTenantMapRepository.GetQueryableAsync();
                var existingMappings = await mapQueryable
                    .Where(m => allSubUsernames.Contains(m.OidcSubUsername))
                    .ToListAsync();

                var existingByKey = existingMappings
                    .ToDictionary(m => (m.OidcSubUsername, m.TenantId));

                foreach (var (subUsername, tenantId, tenantName) in desiredMappings)
                {
                    if (existingByKey.TryGetValue((subUsername, tenantId), out var existing))
                    {
                        existing.LastUpdated = DateTime.UtcNow;
                        await applicantTenantMapRepository.UpdateAsync(existing);
                        totalMappingsUpdated++;
                    }
                    else
                    {
                        var newMapping = new ApplicantTenantMap
                        {
                            OidcSubUsername = subUsername,
                            TenantId = tenantId,
                            TenantName = tenantName,
                            LastUpdated = DateTime.UtcNow
                        };
                        await applicantTenantMapRepository.InsertAsync(newMapping);
                        existingByKey[(subUsername, tenantId)] = newMapping;
                        totalMappingsCreated++;
                        Logger.LogInformation("Created missing ApplicantTenantMap for {SubUsername} in tenant {TenantName}",
                            subUsername, tenantName);
                    }
                }
            }

            Logger.LogInformation("ApplicantTenantMap reconciliation completed. Created: {Created}, Updated: {Updated}",
                totalMappingsCreated, totalMappingsUpdated);

            return (totalMappingsCreated, totalMappingsUpdated);
        }
    }
}
