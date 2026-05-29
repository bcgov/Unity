using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applicants;
using Unity.Notifications.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Internal query service that backs <c>ApplicantProfileController</c>. Aggregates applicant profile
    /// data from registered <see cref="IApplicantProfileDataProvider"/> implementations and resolves
    /// the applicant's tenant mappings. Not exposed as an ABP application service: the HTTP surface
    /// lives in the controller, which applies its own routing and API-key authorization.
    /// </summary>
    public class ApplicantProfileQueryService(
            ICurrentTenant currentTenant,
            IRepository<ApplicantTenantMap, Guid> applicantTenantMapRepository,
            IEnumerable<IApplicantProfileDataProvider> dataProviders,
            ISettingProvider settingProvider,
            ILogger<ApplicantProfileQueryService> logger)
        : IApplicantProfileQueryService, ITransientDependency
    {
        private readonly Dictionary<string, IApplicantProfileDataProvider> _providersByKey
            = dataProviders.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Retrieves the applicant's profile information based on the specified request.
        /// </summary>
        /// <param name="request">An object containing the criteria used to identify the applicant profile to retrieve. Must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see
        /// cref="ApplicantProfileDto"/> with the applicant's profile data.</returns>
        public async Task<ApplicantProfileDto> GetApplicantProfileAsync(ApplicantProfileInfoRequest request)
        {
            var dto = new ApplicantProfileDto
            {
                ProfileId = request.ProfileId,
                Subject = request.Subject,
                TenantId = request.TenantId,
                Key = request.Key
            };

            if (_providersByKey.TryGetValue(request.Key, out var provider))
            {
                dto.Data = await provider.GetDataAsync(request);
            }
            else
            {
                logger.LogWarning("Unknown applicant profile key provided");
            }

            return dto;
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
            var subUsername = SubjectNormalizer.Normalize(request.Subject);
            if (subUsername is null) return [];
            List<ApplicantTenantDto> mappings = [];

            // Query the ApplicantTenantMaps table in the host database
            using (currentTenant.Change(null))
            {
                var queryable = await applicantTenantMapRepository.GetQueryableAsync();
                mappings = await queryable
                    .Where(m => m.OidcSubUsername == subUsername)
                    .Select(m => new ApplicantTenantDto
                    {
                        TenantId = m.TenantId,
                        TenantName = m.TenantName
                    })
                    .ToListAsync();
            }

            // Apply tenant specific metadata
            foreach (var map in mappings)
            {
                await AddTenantMetadataAsync(map);
            }

            return mappings;
        }

        /// <summary>
        /// Add on any relevant tenant specific metadata
        /// </summary>
        /// <param name="tenantMap">The applicant tenant DTO to enrich with tenant-specific metadata.</param>
        private async Task AddTenantMetadataAsync(ApplicantTenantDto tenantMap)
        {
            using (currentTenant.Change(tenantMap.TenantId))
            {
                var defaultEmailAddress = await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                tenantMap.Metadata[ApplicantTenantMetadataKeys.DefaultFromAddress] = defaultEmailAddress ?? "NoReply@gov.bc.ca";
            }
        }
    }
}
