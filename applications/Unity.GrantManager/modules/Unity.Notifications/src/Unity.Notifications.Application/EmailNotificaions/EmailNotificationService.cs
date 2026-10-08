using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Renderers;
using Unity.GrantManager.Notifications;
using Unity.Notifications.Emails;
using Unity.Notifications.EmailAddresses;
using Unity.Notifications.Permissions;
using Unity.Notifications.Settings;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.SettingManagement;
using Volo.Abp.Users;

namespace Unity.Notifications.EmailNotifications;

[Dependency(ReplaceServices = false)]
[ExposeServices(typeof(EmailNotificationService), typeof(IEmailNotificationService))]
public class EmailNotificationService(
        INotificationsAppService notificationAppService,
        EmailNotificationManager emailNotificationManager,
        IExternalUserLookupServiceProvider externalUserLookupServiceProvider,
        ISettingManager settingManager,
        IFeatureChecker featureChecker,
        IConfiguration configuration,
        IMarkdownRenderer markdownRenderer,
        IEmailAddressConfigurationsRepository emailAddressConfigurationsRepository,
        EmailComposerAccessChecker emailAccessChecker,
        IEmailLogsRepository emailLogsRepository) : ApplicationService, IEmailNotificationService
{

    [Authorize(NotificationsPermissions.Email.Send)]
    public async Task<Guid> InitializeDraftAsync(Guid applicationId)
    {
        await emailAccessChecker.CheckOwnerAsync(applicationId, Guid.Empty, NotificationsPermissions.Email.Send);
        var emailLog = await emailNotificationManager.CreateDraftEmailLogAsync(applicationId);
        return emailLog.Id;
    }

    [Authorize]
    public async Task DeleteEmail(Guid id)
    {
        var email = await emailLogsRepository.GetAsync(id);
        // Closing an unsaved composer cleans up only the current user's empty draft.
        var isUnsavedOwnDraft = email.Status == EmailStatus.Draft && email.CreatorId == CurrentUser.Id
            && string.IsNullOrEmpty(email.Subject) && string.IsNullOrEmpty(email.Body);
        var permission = email.Status == EmailStatus.Draft
            ? (isUnsavedOwnDraft ? NotificationsPermissions.Email.Send : NotificationsPermissions.Email.DeleteDraft)
            : NotificationsPermissions.Email.CancelScheduled;
        await emailAccessChecker.CheckOwnerAsync(email.ApplicationId, email.ApplicantId, permission);
        if (email.Status != EmailStatus.Draft && (!email.SendOnDateTime.HasValue || email.SendOnDateTime <= Clock.Now))
        {
            throw new BusinessException("Notifications:EmailNotScheduled");
        }
        await emailNotificationManager.DeleteEmailLogAsync(id);
    }

    [Authorize(NotificationsPermissions.Email.CancelScheduled)]
    public async Task CancelEmail(Guid id)
    {
        var email = await emailAccessChecker.CheckEmailAsync(id, NotificationsPermissions.Email.CancelScheduled);
        if (!email.SendOnDateTime.HasValue || email.SendOnDateTime <= Clock.Now)
        {
            throw new BusinessException("Notifications:EmailNotScheduled");
        }
        await emailNotificationManager.CancelEmailLogAsync(id);
    }

    public async Task<int> GetEmailsChesWithNoResponseCountAsync()
    {
        return await emailNotificationManager.GetPendingEmailsCountAsync();
    }

    [RemoteService(false)]
    public async Task<EmailLog?> UpdateEmailLog(Guid emailId, EmailMessageParams email, Guid applicationId, string? status, Guid applicantId = default)
    {
        return await emailNotificationManager.UpdateEmailLogAsync(emailId, email, applicationId, status, applicantId);
    }

    [RemoteService(false)]
    public async Task<EmailLog?> InitializeEmailLog(EmailMessageParams email, Guid applicationId)
    {
        return await emailNotificationManager.CreateEmailLogAsync(email, applicationId);
    }

    [RemoteService(false)]
    public async Task<EmailLog?> InitializeEmailLog(EmailMessageParams email, Guid applicationId, string? status, Guid applicantId = default)
    {
        return await emailNotificationManager.CreateEmailLogAsync(email, applicationId, status, applicantId: applicantId);
    }

    protected virtual async Task NotifyTeamsChannel(string chesEmailError)
    {
        string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string activityTitle = "CHES Email error: " + chesEmailError;
        string activitySubtitle = "Environment: " + envInfo;
        await notificationAppService.PostToNotificationsAsync(activityTitle, activitySubtitle);
    }

    public Task<string> GetBaseUrlAsync()
    {
        var selfUrl = configuration["App:SelfUrl"];
        
        if (string.IsNullOrWhiteSpace(selfUrl))
        {
            throw new InvalidOperationException(
                "App:SelfUrl configuration is not set. Cannot resolve base URL for email notifications. " +
                "Ensure the configuration is properly set in appsettings or environment variables.");
        }
        
        return Task.FromResult(selfUrl.TrimEnd('/'));
    }

    [Authorize]
    public async Task<HttpResponseMessage> SendCommentNotification(EmailCommentDto input)
    {
        HttpResponseMessage res = new();
        try
        {
            if (await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                var baseUrl = await GetBaseUrlAsync();

                string commentLink = input.CommentType switch
                {
                    Comments.CommentType.ApplicationComment or Comments.CommentType.AssessmentComment =>
                        QueryHelpers.AddQueryString($"{baseUrl}/GrantApplications/Details", new Dictionary<string, string?>
                        {
                            ["ApplicationId"] = input.OwnerId,
                            ["TenantId"] = CurrentTenant.Id?.ToString()
                        }),
                    Comments.CommentType.ApplicantComment =>
                        QueryHelpers.AddQueryString($"{baseUrl}/GrantApplicants/Details", new Dictionary<string, string?>
                        {
                            ["ApplicantId"] = input.OwnerId,
                            ["TenantId"] = CurrentTenant.Id?.ToString()
                        }),
                    _ => throw new InvalidOperationException("Invalid comment type.")
                };

                var subject = $"Unity-Comment: {input.Subject}";
                var senderAddresses = await emailAddressConfigurationsRepository.GetListAsync(address =>
                    address.EmailType == "Sender" && address.IsActive && address.IsDefault);
                var fromEmail = senderAddresses.FirstOrDefault()?.EmailAddress ?? "NoReply@gov.bc.ca";

                var hasSurname = !string.IsNullOrWhiteSpace(CurrentUser.SurName);
                var hasName = !string.IsNullOrWhiteSpace(CurrentUser.Name);

                var currentUserText = (hasSurname, hasName) switch
                {
                    (true, true) => $"{CurrentUser.SurName}, {CurrentUser.Name}",
                    _ => CurrentUser.UserName ?? "Unknown User"
                };

                string htmlBody = await RenderCommentNotificationTemplateAsync(currentUserText, input.Body, commentLink);

                foreach (var email in input.MentionNamesEmail)
                {
                    var toEmail = email;
                    res = await emailNotificationManager.SendEmailAsync(
                        new EmailMessageParams(toEmail, htmlBody, subject, fromEmail, input.EmailTemplateName), "html");
                }
            }
            else
            {
                res = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Feature is not enabled.")
                };
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "EmailNotificationService->SendEmailCommentNotification: Exception occurred while sending email.");

            res = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent($"An exception occurred while sending the email: {ex.Message}")
            };
        }
        return res;
    }

    /// <summary>
    /// Send Email Notfication
    /// </summary>
    /// <param name="emailTo">The email address to send to</param>
    /// <param name="body">The body of the email</param>
    /// <param name="subject">Subject Message</param>
    /// <param name="emailFrom">From Email Address</param>
    /// <param name="emailBodyType">Type of body email: html or text</param>
    /// <param name="emailTemplateName">Template name for the email</param>
    /// <param name="emailCC">CC email addresses</param>
    /// <param name="emailBCC">BCC email addresses</param>
    /// <returns>HttpResponseMessage indicating the result of the operation</returns>
    [RemoteService(false)]
    public async Task<HttpResponseMessage> SendEmailNotification(EmailMessageParams email, string? emailBodyType = null)
    {
        return await emailNotificationManager.SendEmailAsync(email, emailBodyType);
    }

    /// <summary>
    /// Send Email Notification from EmailLog (with S3 attachments support)
    /// </summary>
    /// <param name="emailLog">The email log containing email details</param>
    /// <returns>HttpResponseMessage indicating the result of the operation</returns>
    [RemoteService(false)]
    public async Task<HttpResponseMessage> SendEmailNotification(EmailLog emailLog)
    {
        return await emailNotificationManager.SendEmailAsync(emailLog);
    }

    [RemoteService(false)]
    public async Task<EmailLog?> GetEmailLogById(Guid id)
    {
        return await emailNotificationManager.GetEmailLogByIdAsync(id);
    }

    [Authorize]
    public virtual async Task<List<EmailHistoryDto>> GetHistoryByApplicationId(Guid applicationId)
    {
        var entityList = await emailNotificationManager.GetEmailLogsByApplicationIdAsync(applicationId);
        return await MapEmailHistoryAsync(entityList);
    }

    [Authorize(NotificationsPermissions.Email.Default)]
    public virtual async Task<List<EmailHistoryDto>> GetHistoryByApplicantId(Guid applicantId)
    {
        await emailAccessChecker.CheckOwnerAsync(Guid.Empty, applicantId, NotificationsPermissions.Email.Default);
        return await MapEmailHistoryAsync(await emailLogsRepository.GetByApplicantIdAsync(applicantId));
    }

    private async Task<List<EmailHistoryDto>> MapEmailHistoryAsync(List<EmailLog> entityList)
    {
        var dtoList = ObjectMapper.Map<List<EmailLog>, List<EmailHistoryDto>>(entityList);

        var templatesByEmail = entityList.ToDictionary(e => e.Id, EmailOwnership.GetTemplateId);
        foreach (var dto in dtoList)
        {
            dto.TemplateId = templatesByEmail[dto.Id];
        }

        var sentByUserIds = dtoList
            .Where(d => d.CreatorId.HasValue)
            .Select(d => d.CreatorId!.Value)
            .Distinct();

        var userDictionary = new Dictionary<Guid, EmailHistoryUserDto>();

        foreach (var userId in sentByUserIds)
        {
            var userInfo = await externalUserLookupServiceProvider.FindByIdAsync(userId);
            if (userInfo != null)
            {
                userDictionary[userId] = ObjectMapper.Map<IUserData, EmailHistoryUserDto>(userInfo);
            }

        }

        foreach (var item in dtoList)
        {
            if (item.CreatorId.HasValue && userDictionary.TryGetValue(item.CreatorId.Value, out var userDto))
            {
                item.SentBy = userDto;
            }
        }

        return dtoList;
    }

    /// <summary>
    /// Send Email To Queue
    /// </summary>
    /// <param name="emailLog">The email log to send to queue</param>
    [RemoteService(false)]
    public async Task SendEmailToQueue(EmailLog emailLog)
    {
        await emailNotificationManager.QueueEmailAsync(emailLog);
    }

    [Authorize(NotificationsPermissions.Settings)]
    public async Task UpdateSettings(NotificationsSettingsDto settingsDto)
    {
        await UpdateTenantSettings(NotificationsSettings.Mailing.EmailMaxRetryAttempts, settingsDto.MaximumRetryAttempts);
        await settingManager.SetForCurrentTenantAsync(NotificationsSettings.Mailing.EnableEmailDelay, settingsDto.EnableEmailDelay ? "true" : "false");
    }

    private async Task UpdateTenantSettings(string settingKey, string valueString)
    {
        if (!valueString.IsNullOrWhiteSpace())
        {
            await settingManager.SetForCurrentTenantAsync(settingKey, valueString);
        }
    }

    /// <summary>
    /// Renders the comment notification email template with the provided parameters.
    /// </summary>
    /// <param name="currentUserText">Display name of the user who mentioned</param>
    /// <param name="commentBody">The comment body text</param>
    /// <param name="commentLink">The URL link to view the comment</param>
    /// <returns>Rendered HTML email body</returns>
    private async Task<string> RenderCommentNotificationTemplateAsync(string currentUserText, string commentBody, string commentLink)
    {
        // Load template from embedded resources or file system
        string templateContent = await LoadEmailTemplateAsync("CommentNotification");

        var encodedCurrentUserText = WebUtility.HtmlEncode(currentUserText);
        var encodedCommentBody = markdownRenderer.Render(commentBody);
        var encodedCommentLink = WebUtility.HtmlEncode(commentLink);

        // Replace placeholders with actual values
        var renderedTemplate = templateContent
            .Replace("@Model.CurrentUserText", encodedCurrentUserText)
            .Replace("@Html.Raw(Model.CommentBody)", encodedCommentBody)
            .Replace("@Model.CommentLink", encodedCommentLink)
            .Replace("@model dynamic", string.Empty);

        return renderedTemplate;
    }

    /// <summary>
    /// Loads an email template from the Application assembly's embedded resources.
    /// The resource name follows the format Unity.Notifications.EmailTemplates.{templateName}.cshtml.
    /// </summary>
    /// <param name="templateName">Template name without extension (e.g., "CommentNotification")</param>
    /// <returns>Template content as a string</returns>
    private async Task<string> LoadEmailTemplateAsync(string templateName)
    {
        try
        {
            var assembly = typeof(EmailNotificationService).Assembly;
            var resourceName = $"Unity.Notifications.EmailTemplates.{templateName}.cshtml";
            await using var templateStream = assembly.GetManifestResourceStream(resourceName);

            if (templateStream == null)
            {
                throw new FileNotFoundException($"Embedded email template not found: {resourceName}");
            }

            using var reader = new StreamReader(templateStream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to load email template '{templateName}'");
            throw;
        }
    }
}
