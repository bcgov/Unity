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
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Settings;
using Unity.Notifications.Emails;
using Unity.Notifications.EmailAddresses;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.Identity;
using Volo.Abp.SettingManagement;
using Volo.Abp.Uow;

namespace Unity.Notifications.EmailNotifications;

[Dependency(ReplaceServices = false)]
[ExposeServices(typeof(CommentNotificationService))]
public class CommentNotificationService(
    EmailNotificationManager emailNotificationManager,
    IFeatureChecker featureChecker,
    IConfiguration configuration,
    IMarkdownRenderer markdownRenderer,
    IIdentityUserRepository userRepository,
    IEmailAddressConfigurationsRepository emailAddressConfigurationsRepository,
    ISettingManager settingManager,
    IAssessmentRepository assessmentRepository) : ApplicationService
{
    private async Task<bool> IsEmailCommentsBlockedAsyncByEmailAddress(string toEmail)
    {
        var toUser = await userRepository.FindByNormalizedEmailAsync(toEmail.ToUpper());
        if (toUser == null)
        {
            return false;
        }

        return await IsEmailPreferenceEnabledAsync(toUser.Id, SettingsConstants.UserEmailSettings.EmailBlockCommentTagging);
    }

    private async Task<bool> IsEmailPreferenceEnabledAsync(Guid userId, string settingName, bool defaultValue = false)
    {
        var value = await settingManager.GetOrNullForUserAsync(settingName, userId);
        return value == null ? defaultValue : bool.Parse(value);
    }

    private Task<string> GetBaseUrlAsync()
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

    [RemoteService(false)]
    [Authorize]
    public async Task<HttpResponseMessage> SendCommentNotification(EmailCommentDto input)
    {
        HttpResponseMessage res = new();
        try
        {
            if (await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                var baseUrl = await GetBaseUrlAsync();
                var (applicationId, applicantId) = await ResolveCommentOwnerAsync(input);

                string commentLink = applicantId != Guid.Empty
                    ? QueryHelpers.AddQueryString($"{baseUrl}/GrantApplicants/Details", new Dictionary<string, string?>
                    {
                        ["ApplicantId"] = applicantId.ToString(),
                        ["TenantId"] = CurrentTenant.Id?.ToString()
                    })
                    : QueryHelpers.AddQueryString($"{baseUrl}/GrantApplications/Details", new Dictionary<string, string?>
                    {
                        ["ApplicationId"] = applicationId.ToString(),
                        ["TenantId"] = CurrentTenant.Id?.ToString()
                    });

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

                // Commit the logs before queueing so the email consumer can load them.
                var emailLogs = new List<EmailLog>();
                using (var uow = UnitOfWorkManager.Begin(requiresNew: true, isTransactional: true))
                {
                    foreach (var toEmail in input.MentionNamesEmail.Where(e => !string.IsNullOrWhiteSpace(e)))
                    {
                        if (await IsEmailCommentsBlockedAsyncByEmailAddress(toEmail))
                        {
                            continue;
                        }
                        emailLogs.Add(await emailNotificationManager.CreateInternalEmailLogAsync(
                            new EmailMessageParams(toEmail, htmlBody, subject, fromEmail, input.EmailTemplateName),
                            applicationId, applicantId, EmailType.EventBased));
                    }
                    await uow.CompleteAsync();
                }

                foreach (var emailLog in emailLogs)
                {
                    await emailNotificationManager.QueueEmailAsync(emailLog);
                }

                res = new HttpResponseMessage(HttpStatusCode.Accepted);
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
            Logger.LogError(ex, "CommentNotificationService->SendCommentNotification: Exception occurred while sending email.");
            res = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent($"An exception occurred while sending the email: {ex.Message}")
            };
        }
        return res;
    }

    private async Task<(Guid ApplicationId, Guid ApplicantId)> ResolveCommentOwnerAsync(EmailCommentDto input)
    {
        if (!Guid.TryParse(input.OwnerId, out var ownerId) || ownerId == Guid.Empty)
        {
            throw new InvalidOperationException("Invalid comment owner.");
        }

        return input.CommentType switch
        {
            Comments.CommentType.ApplicationComment => (ownerId, Guid.Empty),
            Comments.CommentType.AssessmentComment => ((await assessmentRepository.GetAsync(ownerId)).ApplicationId, Guid.Empty),
            Comments.CommentType.ApplicantComment => (Guid.Empty, ownerId),
            _ => throw new InvalidOperationException("Invalid comment type.")
        };
    }

    private async Task<string> RenderCommentNotificationTemplateAsync(string currentUserText, string commentBody, string commentLink)
    {
        string templateContent = await LoadEmailTemplateAsync("CommentNotification");

        var encodedCurrentUserText = WebUtility.HtmlEncode(currentUserText);
        var encodedCommentBody = markdownRenderer.Render(commentBody);
        var encodedCommentLink = WebUtility.HtmlEncode(commentLink);

        return templateContent
            .Replace("@Model.CurrentUserText", encodedCurrentUserText)
            .Replace("@Html.Raw(Model.CommentBody)", encodedCommentBody)
            .Replace("@Model.CommentLink", encodedCommentLink)
            .Replace("@model dynamic", string.Empty);
    }

    private async Task<string> LoadEmailTemplateAsync(string templateName)
    {
        try
        {
            var assembly = typeof(CommentNotificationService).Assembly;
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
