using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.BulkEmailNotifications;

public class ComposeAndSendEmailModalModel(
    IBulkEmailNotificationAppService bulkEmailNotificationAppService,
    ApplicationIdsCacheService cacheService) : AbpPageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public List<ComposeEmailApplicationDto> Applications { get; private set; } = [];
    public string ApplicationsJson { get; private set; } = "[]";
    public Guid InitialApplicationId { get; private set; }
    public Guid CurrentUserId { get; private set; }
    public string CurrentUserDisplayName { get; private set; } = string.Empty;

    [BindProperty]
    public string ComposeRequestJson { get; set; } = string.Empty;

    public async Task OnGetAsync(string cacheKey)
    {
        try
        {
            var selectedApplicationIds = await cacheService.GetApplicationIdsAsync(cacheKey);
            if (selectedApplicationIds == null || selectedApplicationIds.Count == 0)
            {
                Logger.Log<string>(LogLevel.Warning, default, $"Cache key expired or invalid for composed bulk email: {cacheKey.SanitizeField()}", null, (s, e) => s);
                ViewData["Error"] = "The session has expired. Please try selecting applications again.";
                return;
            }

            await cacheService.RemoveAsync(cacheKey);

            if (selectedApplicationIds.Count > BatchApprovalConsts.MaxBatchCount)
            {
                ViewData["Error"] = $"A maximum of {BatchApprovalConsts.MaxBatchCount} applications can be emailed at once.";
                return;
            }

            Applications = await bulkEmailNotificationAppService.GetApplicationsForComposeEmail([.. selectedApplicationIds]);
            if (Applications.Count == 0)
            {
                ViewData["Error"] = "None of the selected applications could be loaded.";
                return;
            }

            InitialApplicationId = Applications[0].ApplicationId;
            CurrentUserId = CurrentUser.Id ?? Guid.Empty;
            CurrentUserDisplayName = $"{CurrentUser.Name} {CurrentUser.SurName}".Trim();
            if (string.IsNullOrWhiteSpace(CurrentUserDisplayName))
            {
                CurrentUserDisplayName = CurrentUser.UserName ?? string.Empty;
            }
            ApplicationsJson = JsonSerializer.Serialize(Applications, JsonOptions);
        }
        catch (Exception ex)
        {
            Logger.Log<string>(LogLevel.Error, default, "Error loading Compose & Send Email modal", ex, (s, e) => s);
            ViewData["Error"] = "An error occurred while loading the email form. Please try again.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var request = JsonSerializer.Deserialize<ComposeBulkEmailRequestDto>(ComposeRequestJson, JsonOptions);
            if (request == null || request.Emails.Count == 0)
            {
                return BadRequest("At least one composed email is required.");
            }

            var result = await bulkEmailNotificationAppService.SendComposedBulkEmails(request);
            return new OkObjectResult(result);
        }
        catch (JsonException ex)
        {
            Logger.Log<string>(LogLevel.Warning, default, "Invalid Compose & Send Email request payload", ex, (s, e) => s);
            return BadRequest("The composed email request is invalid.");
        }
        catch (Exception ex)
        {
            Logger.Log<string>(LogLevel.Error, default, "Error sending composed bulk emails", ex, (s, e) => s);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
