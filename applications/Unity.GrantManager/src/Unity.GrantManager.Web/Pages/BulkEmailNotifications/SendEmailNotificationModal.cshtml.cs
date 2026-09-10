using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Pages.BulkEmailNotifications.ViewModels;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.BulkEmailNotifications;

public class SendEmailNotificationModalModel(IBulkEmailNotificationAppService bulkEmailNotificationAppService,
    ApplicationIdsCacheService cacheService) : AbpPageModel
{
    [BindProperty]
    public List<BulkEmailNotificationViewModel>? BulkEmailNotifications { get; set; }

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
        MaxBatchCount = BatchApprovalConsts.MaxBatchCount;
        BulkEmailNotifications = [];
        MaxBatchCountExceededError = L["SendEmailNotificationRequest:MaxCountExceeded", BatchApprovalConsts.MaxBatchCount.ToString()].Value;

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

            Guid[] applicationGuids = selectedApplicationIds.ToArray();

            // Clean up cache after retrieval (one-time use)
            await cacheService.RemoveAsync(cacheKey);

            if (!ValidCount(applicationGuids))
            {
                MaxBatchCountExceeded = true;
            }

            if (applicationGuids.Length == 0)
            {
                return;
            }

            var applications = await bulkEmailNotificationAppService.GetApplicationsForBulkEmail(applicationGuids);

            foreach (var application in applications)
            {
                var bulkEmailNotification = new BulkEmailNotificationViewModel
                {
                    ApplicationId = application.ApplicationId,
                    EmailId = application.EmailId,
                    ReferenceNo = application.ReferenceNo,
                    ApplicantName = application.ApplicantName,
                    EmailSubject = application.EmailSubject,
                    ApplicationStatus = application.ApplicationStatus,
                    FormName = application.FormName,
                    ApprovedAmount = application.ApprovedAmount,
                    DecisionDate = application.DecisionDate,
                    CreatedByName = application.CreatedByName,
                    LastModified = application.LastModified,
                    IsValid = application.IsValid
                };

                SetNotes(application, bulkEmailNotification);
                BulkEmailNotifications.Add(bulkEmailNotification);
            }

            Invalid = applications.Exists(s => !s.IsValid) || MaxBatchCountExceeded;
            ApplicationsCount = applications.Count;

            Logger.LogInformation("Successfully loaded bulk email modal for {Count} applications", selectedApplicationIds.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading bulk email modal");
            ViewData["Error"] = "An error occurred while loading the email form. Please try again.";
            Invalid = true;
        }
    }

    private void SetNotes(BulkEmailNotificationDto application, BulkEmailNotificationViewModel bulkEmailNotification)
    {
        List<EmailNotificationNoteViewModel> notes = EmailNotificationNoteViewModel.CreateNotesList(localizer: L);

        foreach (var validation in application.ValidationMessages)
        {
            var index = notes.FindIndex(note => note.Key == validation);
            if (index != -1)
            {
                notes[index] = new EmailNotificationNoteViewModel(validation, true, notes[index].Description, notes[index].IsError);
            }
        }

        bulkEmailNotification.Notes = notes;
    }

    /// <summary>
    /// Re-validate a single application's draft state (called after a Save in the right-hand edit panel)
    /// so the row's validity/notes/Created By/Last Modified can be refreshed without reloading the whole batch.
    /// </summary>
    public async Task<IActionResult> OnGetRevalidateAsync(Guid applicationId)
    {
        try
        {
            var application = await bulkEmailNotificationAppService.RevalidateApplicationForBulkEmail(applicationId);

            var viewModel = new BulkEmailNotificationViewModel { ApplicationId = application.ApplicationId };
            SetNotes(application, viewModel);

            return new OkObjectResult(new
            {
                applicationId = application.ApplicationId,
                emailId = application.EmailId,
                isValid = application.IsValid,
                createdByName = application.CreatedByName,
                lastModified = application.LastModified,
                notes = viewModel.Notes
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error revalidating application {ApplicationId} for bulk email", applicationId);
            // A 2xx status (including 204 No Content) is treated as success by the client's $.ajax().done()
            // handler, which immediately dereferences fields on the response body — an empty success response
            // would throw client-side. Return a genuine error status so it lands in .fail() instead.
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (BulkEmailNotifications == null) return NoContent();

            var emailRequests = MapBulkEmailRequests();

            var result = await bulkEmailNotificationAppService.SendBulkEmailNotifications(emailRequests);

            return new OkObjectResult(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error sending bulk email notifications");
        }

        return NoContent();
    }

    private List<BulkEmailNotificationDto> MapBulkEmailRequests()
    {
        var bulkEmailNotifications = new List<BulkEmailNotificationDto>();

        foreach (var application in BulkEmailNotifications ?? [])
        {
            bulkEmailNotifications.Add(new BulkEmailNotificationDto()
            {
                ApplicationId = application.ApplicationId,
                EmailId = application.EmailId,
                ReferenceNo = application.ReferenceNo,
                ApplicantName = application.ApplicantName ?? string.Empty,
                ValidationMessages = []
            });
        }

        return bulkEmailNotifications;
    }

    private static bool ValidCount(Guid[] applicationGuids)
    {
        return applicationGuids.Length <= BatchApprovalConsts.MaxBatchCount;
    }
}
