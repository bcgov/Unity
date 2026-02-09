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
                ? request.Subject[..request.Subject.IndexOf('@')].ToUpper()
                : request.Subject.ToUpper();

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
        /// and ensuring mappings exist in the host database
        /// </summary>
        /// <returns>Tuple of (created count, updated count)</returns>
        public async Task<(int Created, int Updated)> ReconcileApplicantTenantMapsAsync()
        {
            Logger.LogInformation("Starting ApplicantTenantMap reconciliation...");

            int totalMappingsCreated = 0;
            int totalMappingsUpdated = 0;

            var tenants = await tenantRepository.GetListAsync();

            foreach (var tenant in tenants)
            {
                try
                {
                    Logger.LogDebug("Processing tenant: {TenantName}", tenant.Name);

                    // Get distinct OidcSub values from this tenant's submissions
                    using (currentTenant.Change(tenant.Id))
                    {
                        var submissionQueryable = await applicationFormSubmissionRepository.GetQueryableAsync();
                        var distinctOidcSubs = await submissionQueryable
                            .Where(s => !string.IsNullOrEmpty(s.OidcSub))
                            .Select(s => s.OidcSub)
                            .Distinct()
                            .ToListAsync();

                        // For each distinct OidcSub, ensure mapping exists in host database
                        using (currentTenant.Change(null))
                        {
                            foreach (var oidcSub in distinctOidcSubs)
                            {
                                var subUsername = oidcSub.Contains('@')
                                    ? oidcSub[..oidcSub.IndexOf('@')].ToUpper()
                                    : oidcSub.ToUpper();

                                var mapQueryable = await applicantTenantMapRepository.GetQueryableAsync();
                                var existingMapping = await mapQueryable
                                    .FirstOrDefaultAsync(m => m.OidcSubUsername == subUsername && m.TenantId == tenant.Id);

                                if (existingMapping != null)
                                {
                                    // Update LastUpdated
                                    existingMapping.LastUpdated = DateTime.UtcNow;
                                    await applicantTenantMapRepository.UpdateAsync(existingMapping);
                                    totalMappingsUpdated++;
                                }
                                else
                                {
                                    // Create new mapping
                                    var newMapping = new ApplicantTenantMap
                                    {
                                        OidcSubUsername = subUsername,
                                        TenantId = tenant.Id,
                                        TenantName = tenant.Name,
                                        LastUpdated = DateTime.UtcNow
                                    };
                                    await applicantTenantMapRepository.InsertAsync(newMapping);
                                    totalMappingsCreated++;
                                    Logger.LogInformation("Created missing ApplicantTenantMap for {SubUsername} in tenant {TenantName}",
                                        subUsername, tenant.Name);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error reconciling ApplicantTenantMaps for tenant {TenantName}", tenant.Name);
                }
            }

            Logger.LogInformation("ApplicantTenantMap reconciliation completed. Created: {Created}, Updated: {Updated}",
                totalMappingsCreated, totalMappingsUpdated);

            return (totalMappingsCreated, totalMappingsUpdated);
        }
    }
}
