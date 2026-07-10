using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.BulkActions;

[Authorize(UnitySelector.Application.Status.BulkPublish)]
public class BulkPublishApplicationsModel(
    ApplicationIdsCacheService cacheService,
    IBulkApprovalsAppService bulkApprovalsAppService) : AbpPageModel
{
    [BindProperty]
    public List<BulkPublishApplicationViewModel>? BulkApplications { get; set; }

    [BindProperty]
    public string SelectedApplicationIds { get; set; } = string.Empty;

    [TempData]
    public int ApplicationsCount { get; set; }

    [TempData]
    public bool Invalid { get; set; }

    [TempData]
    public int MaxBatchCount { get; set; }

    [TempData]
    public string? MaxBatchCountExceededError { get; set; }

    [TempData]
    public bool MaxBatchCountExceeded { get; set; }

    public async Task OnGetAsync(string cacheKey)
    {
        if (!await AuthorizationService.IsGrantedAsync(UnitySelector.Application.Status.BulkPublish))
        {
            return;
        }

        MaxBatchCount = BatchApprovalConsts.MaxBatchCount;
        BulkApplications = [];
        MaxBatchCountExceededError = L["ApplicationBatchPublishRequest:MaxCountExceeded", BatchApprovalConsts.MaxBatchCount.ToString()].Value;

        try
        {
            // Retrieve application IDs from distributed cache
            var selectedApplicationIds = await cacheService.GetApplicationIdsAsync(cacheKey);

            if (selectedApplicationIds == null || selectedApplicationIds.Count == 0)
            {
                Logger.LogWarning("Cache key expired or invalid: {CacheKey}", cacheKey.SanitizeField());
                ViewData["Error"] = "The session has expired. Please try selecting applications again.";
                Invalid = true;
                return;
            }

            // Convert List<Guid> to Guid[] for existing code compatibility
            Guid[] applicationGuids = [.. selectedApplicationIds];

            // Clean up cache after retrieval (one-time use)
            await cacheService.RemoveAsync(cacheKey);

            if (applicationGuids.Length > MaxBatchCount)
            {
                MaxBatchCountExceeded = true;
            }

            if (applicationGuids.Length == 0)
            {
                Invalid = true;
                return;
            }

            // Fetch application details for the selected IDs
            var bulkApplications = await bulkApprovalsAppService.GetApplicationsForBulkPublish(applicationGuids);

            BulkApplications = ObjectMapper.Map<List<BulkPublishDto>, List<BulkPublishApplicationViewModel>>(bulkApplications);
            ApplicationsCount = BulkApplications.Count;
            Invalid = MaxBatchCountExceeded || ApplicationsCount == 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading bulk publish applications modal");
            ViewData["Error"] = "An error occurred while loading the applications. Please try again.";
            Invalid = true;
            return;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (BulkApplications == null) return NoContent();
            if (BulkApplications.Count == 0 || BulkApplications.Count > BatchApprovalConsts.MaxBatchCount)
            {
                return BadRequest();
            }

            var applicationsToPublish = BulkApplications.Select(y => y.ApplicationId).ToArray();
            await bulkApprovalsAppService.BulkPublishApplications(applicationsToPublish);

            return new OkResult();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating application status");
        }

        return NoContent();
    }
}
