using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applications;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ApplicationBulkActionsAppService), typeof(IApplicationBulkActionsAppService))]
[Authorize]
public class ApplicationBulkActionsAppService(
    ApplicationIdsCacheService cacheService,
    IApplicationRepository applicationRepository,
    IApplicationStatusRepository applicationStatusRepository,
    IApplicationFormRepository applicationFormRepository) : GrantManagerAppService, IApplicationBulkActionsAppService
{

    /// <summary>
    /// Stores application IDs in distributed cache for bulk operations
    /// </summary>
    /// <param name="input">Request containing list of application IDs</param>
    /// <returns>Cache key to retrieve the stored IDs</returns>
    public async Task<StoreApplicationIdsResultDto> StoreApplicationIdsAsync(StoreApplicationIdsRequestDto input)
    {
        if (input == null || input.ApplicationIds == null || input.ApplicationIds.Count == 0)
        {
            throw new UserFriendlyException("No application IDs provided");
        }

        try
        {
            var cacheKey = await cacheService.StoreApplicationIdsAsync(input.ApplicationIds);

            Logger.LogInformation(
                "User {UserId} stored {Count} application IDs for bulk operation with cache key: {CacheKey}",
                CurrentUser?.Id,
                input.ApplicationIds.Count,
                cacheKey);

            return new StoreApplicationIdsResultDto
            {
                CacheKey = cacheKey
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to store application IDs for bulk operation");
            throw new UserFriendlyException("Failed to prepare bulk operation. Please try again.");
        }
    }

    /// <summary>
    /// Get applications for bulk publish with internal and external status information
    /// </summary>
    /// <param name="applicationGuids"></param>
    /// <param name="excludePublished"></param>
    /// <returns></returns>
    [Authorize(UnitySelector.Application.Status.BulkPublish)]
    public async Task<List<BulkPublishDto>> GetApplicationsForBulkPublish(Guid[] applicationGuids, bool excludePublished = true)
    {
        var applicationsQuery = await applicationRepository.GetQueryableAsync();
        var statusesQuery = await applicationStatusRepository.GetQueryableAsync();
        var formsQuery = await applicationFormRepository.GetQueryableAsync();

        var results = await (
            from application in applicationsQuery
            join form in formsQuery on application.ApplicationFormId equals form.Id
            join status in statusesQuery on application.ApplicationStatusId equals status.Id
            where applicationGuids.Contains(application.Id) && (!excludePublished || !application.ExternalStatusVisibility)
            select new BulkPublishDto
            {
                ApplicationId = application.Id,
                ReferenceNo = application.ReferenceNo,
                ApplicantName = application.Applicant.ApplicantName ?? string.Empty,
                ApplicationStatus = status.InternalStatus,
                FormName = form.ApplicationFormName ?? string.Empty,
                FinalDecisionDate = application.FinalDecisionDate,
                ExternalStatusVisibility = application.ExternalStatusVisibility,
                ExternalStatus = application.ExternalStatusVisibility
                    ? status.NotifiedStatus ?? status.ExternalStatus
                    : status.ExternalStatus,
                PublishedStatus = status.NotifiedStatus ?? status.ExternalStatus,
            }).ToListAsync();

        return results;
    }

    /// <summary>
    /// Bulk publish applications by setting the ExternalStatusVisibility to true for the specified application GUIDs.
    /// </summary>
    /// <param name="applicationGuids">The GUIDs of the applications to be bulk published.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Authorize(UnitySelector.Application.Status.BulkPublish)]
    public async Task BulkPublishApplications(Guid[] applicationGuids)
    {
        var applications = await applicationRepository
                .GetListAsync(x => applicationGuids.Contains(x.Id) && !x.ExternalStatusVisibility);

        foreach (var application in applications)
        {
            application.ExternalStatusVisibility = true;
            await applicationRepository.UpdateAsync(application);
        }
    }
}
