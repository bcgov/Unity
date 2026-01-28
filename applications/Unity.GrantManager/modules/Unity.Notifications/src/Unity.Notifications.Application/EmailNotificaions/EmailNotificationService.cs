using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Unity.Notifications.Permissions;
using Unity.Notifications.Settings;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;
using Volo.Abp.SettingManagement;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Users;
using Unity.GrantManager.Notifications;

namespace Unity.Notifications.EmailNotifications;

[Dependency(ReplaceServices = false)]
[ExposeServices(typeof(EmailNotificationService), typeof(IEmailNotificationService))]
public class EmailNotificationService(
        INotificationsAppService notificationAppService,
        EmailNotificationManager emailNotificationManager,
        IExternalUserLookupServiceProvider externalUserLookupServiceProvider,
        ISettingManager settingManager,
        IHttpContextAccessor httpContextAccessor,
        IFeatureChecker featureChecker) : ApplicationService, IEmailNotificationService
{

    public async Task DeleteEmail(Guid id)
    {
        await emailNotificationManager.DeleteEmailLogAsync(id);
    }

    public async Task<int> GetEmailsChesWithNoResponseCountAsync()
    {
        return await emailNotificationManager.GetPendingEmailsCountAsync();
    }

    public async Task<EmailLog?> UpdateEmailLog(Guid emailId, string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
    {
        return await emailNotificationManager.UpdateEmailLogAsync(emailId, emailTo, body, subject, applicationId, emailFrom, status, emailTemplateName, emailCC, emailBCC);
    }

    public async Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
    {
        return await emailNotificationManager.CreateEmailLogAsync(emailTo, body, subject, applicationId, emailFrom, emailTemplateName, emailCC, emailBCC);
    }

    [RemoteService(false)]
    public async Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
    {
        return await emailNotificationManager.CreateEmailLogAsync(emailTo, body, subject, applicationId, emailFrom, status, emailTemplateName, emailCC, emailBCC);
    }

    protected virtual async Task NotifyTeamsChannel(string chesEmailError)
    {
        string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string activityTitle = "CHES Email error: " + chesEmailError;
        string activitySubtitle = "Environment: " + envInfo;
        await notificationAppService.PostToTeamsAsync(activityTitle, activitySubtitle);
    }

    public async Task<HttpResponseMessage> SendCommentNotification(EmailCommentDto input)
    {
        HttpResponseMessage res = new();
        try
        {
            if (await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                var defaultFromAddress = await SettingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);
                var scheme = "https";
                var request = (httpContextAccessor.HttpContext?.Request) ?? throw new InvalidOperationException("HttpContext or Request is null.");
                var host = request.Host.ToUriComponent();
                var pathBase = "/GrantApplications/Details?ApplicationId=";
                var baseUrl = $"{scheme}://{host}{pathBase}";
                var commentLink = $"{baseUrl}{input.ApplicationId}";
                var subject = $"Unity-Comment: {input.Subject}";
                var fromEmail = defaultFromAddress ?? "NoReply@gov.bc.ca";
                string htmlBody = $@"
                <html lang='en' xmlns='http://www.w3.org/1999/xhtml' xmlns:v='urn:schemas-microsoft-com:vml' xmlns:o='urn:schemas-microsoft-com:office:office'>
                <body style='font-family: Arial, sans-serif;'>
                    <h3 style='color: #0a58ca;'>{input.From} mentioned you in a comment.</h3>
                    <table style='width: 100%; background-color: #f9f9f9; border-left: 3px solid #ccc;'>
                        <tr>
                            <td style='padding: 15px;'>
                                <p>{input.Body}</p>
                            </td>
                        </tr>
                    </table>
                    <br />
                    <table style='background-color: #255a90;'>
                        <tr>
                            <td style='padding: 5px 10px; color: #fff; border: 1px solid #2d63c8'>
                                <a href='{commentLink}' target='_blank'
                                    style='display: inline-block;
                                    font-size: 14px;
                                    color: #fff;
                                    text-decoration: none;'>View Comment</a>
                            </td>
                        </tr>
                    </table>
                    <p style='font-size: 12px; color: #999;'>*Note - Please do not reply to this email as it is an automated notification.</p>
                </body>
                </html>";

                foreach (var email in input.MentionNamesEmail)
                {
                    var toEmail = email;
                    res = await emailNotificationManager.SendEmailAsync(toEmail, htmlBody, subject, fromEmail, "html", input.EmailTemplateName);
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
    public async Task<HttpResponseMessage> SendEmailNotification(string emailTo, string body, string subject, string? emailFrom, string? emailBodyType, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
    {
        return await emailNotificationManager.SendEmailAsync(emailTo, body, subject, emailFrom, emailBodyType, emailTemplateName, emailCC, emailBCC);
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

    public async Task<EmailLog?> GetEmailLogById(Guid id)
    {
        return await emailNotificationManager.GetEmailLogByIdAsync(id);
    }

    [Authorize]
    public virtual async Task<List<EmailHistoryDto>> GetHistoryByApplicationId(Guid applicationId)
    {
        var entityList = await emailNotificationManager.GetEmailLogsByApplicationIdAsync(applicationId);
        var dtoList = ObjectMapper.Map<List<EmailLog>, List<EmailHistoryDto>>(entityList);

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
    public async Task SendEmailToQueue(EmailLog emailLog)
    {
        await emailNotificationManager.QueueEmailAsync(emailLog);
    }

    [Authorize(NotificationsPermissions.Settings)]
    public async Task UpdateSettings(NotificationsSettingsDto settingsDto)
    {
        await UpdateTenantSettings(NotificationsSettings.Mailing.DefaultFromAddress, settingsDto.DefaultFromAddress);
        await UpdateTenantSettings(NotificationsSettings.Mailing.EmailMaxRetryAttempts, settingsDto.MaximumRetryAttempts);
    }

    private async Task UpdateTenantSettings(string settingKey, string valueString)
    {
        if (!valueString.IsNullOrWhiteSpace())
        {
            await settingManager.SetForCurrentTenantAsync(settingKey, valueString);
        }
    }
}
