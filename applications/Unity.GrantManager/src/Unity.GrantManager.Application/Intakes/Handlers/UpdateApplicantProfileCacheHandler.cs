using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applicants;
using Unity.GrantManager.Intakes.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Intakes.Handlers
{
    public class UpdateApplicantProfileCacheHandler(
        IRepository<ApplicantTenantMap, Guid> applicantTenantMapRepository,
        ICurrentTenant currentTenant,
        ITenantRepository tenantRepository,
        ILogger<UpdateApplicantProfileCacheHandler> logger)
        : ILocalEventHandler<ApplicationProcessEvent>, ITransientDependency
    {
        /// <summary>
        /// Handles an application process event by updating or creating an applicant-to-tenant mapping based on the
        /// event data.
        /// </summary>
        /// <remarks>If the mapping for the applicant and tenant already exists, the method updates its
        /// last updated timestamp; otherwise, it creates a new mapping. If required data is missing from the event, the
        /// method returns without performing any operation. Errors encountered during processing are logged but not
        /// propagated.</remarks>
        /// <param name="eventData">The event data containing information about the application and form submission. Cannot be null and must
        /// include valid Application and ApplicationFormSubmission objects.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task HandleEventAsync(ApplicationProcessEvent eventData)
        {
            if (eventData.ApplicationFormSubmission == null || eventData.Application == null)
            {
                return;
            }

            try
            {
                var submission = eventData.ApplicationFormSubmission;
                var subUsername = submission.OidcSub.Contains('@')
                    ? submission.OidcSub[..submission.OidcSub.IndexOf('@')].ToUpper()
                    : submission.OidcSub.ToUpper();

                if (string.IsNullOrWhiteSpace(subUsername))
                {
                    logger.LogWarning("OidcSub is empty for submission {SubmissionId}", submission.Id);
                    return;
                }

                var tenantId = submission.TenantId ?? currentTenant.Id;
                if (tenantId == null)
                {
                    logger.LogWarning("Unable to determine tenant for submission {SubmissionId}", submission.Id);
                    return;
                }

                // Get tenant name from host context
                string tenantName;
                using (currentTenant.Change(null))
                {
                    var tenant = await tenantRepository.GetAsync(tenantId.Value);
                    tenantName = tenant.Name;

                    // Check if mapping already exists
                    var queryable = await applicantTenantMapRepository.GetQueryableAsync();
                    var existingMapping = queryable
                        .FirstOrDefault(m => m.OidcSubUsername == subUsername && m.TenantId == tenantId.Value);

                    if (existingMapping != null)
                    {
                        // Update LastUpdated timestamp
                        existingMapping.LastUpdated = DateTime.UtcNow;
                        await applicantTenantMapRepository.UpdateAsync(existingMapping);
                        logger.LogDebug("Updated ApplicantTenantMap for {SubUsername} in tenant {TenantName}", subUsername, tenantName);
                    }
                    else
                    {
                        // Create new mapping
                        var newMapping = new ApplicantTenantMap
                        {
                            OidcSubUsername = subUsername,
                            TenantId = tenantId.Value,
                            TenantName = tenantName,
                            LastUpdated = DateTime.UtcNow
                        };
                        await applicantTenantMapRepository.InsertAsync(newMapping);
                        logger.LogInformation("Created ApplicantTenantMap for {SubUsername} in tenant {TenantName}", subUsername, tenantName);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating ApplicantTenantMap for submission {SubmissionId}", 
                    eventData.ApplicationFormSubmission?.Id);
            }
        }
    }
}

