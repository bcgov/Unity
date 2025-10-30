using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Integrations.Ches;
using Unity.Notifications.Integrations.RabbitMQ;
using Unity.Notifications.Permissions;
using Unity.Notifications.Settings;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Features;
using Volo.Abp.SettingManagement;
using Microsoft.AspNetCore.Http;
using Volo.Abp.Users;
using Unity.GrantManager.Notifications;

namespace Unity.Notifications.EmailNotifications;

[Dependency(ReplaceServices = false)]
[ExposeServices(typeof(EmailNotificationService), typeof(IEmailNotificationService))]
#pragma warning disable S107 // Methods should not have too many parameters
public class EmailNotificationService(
        INotificationsAppService notificationAppService,
        IEmailLogsRepository emailLogsRepository,
        IChesClientService chesClientService,
        EmailQueueService emailQueueService,
        IExternalUserLookupServiceProvider externalUserLookupServiceProvider,
        ISettingManager settingManager,
        IFeatureChecker featureChecker,
        IHttpContextAccessor httpContextAccessor) : ApplicationService, IEmailNotificationService
#pragma warning restore S107 // Methods should not have too many parameters        
{

    public async Task DeleteEmail(Guid id)
    {
        await emailLogsRepository.DeleteAsync(id);
    }    

    public async Task<int> GetEmailsChesWithNoResponseCountAsync()
    {
        var dbNow = DateTime.UtcNow;

        // Create the expression to filter the email logs
        Expression<Func<EmailLog, bool>> filter = x =>
            (x.Status == EmailStatus.Sent && x.ChesResponse == null) ||
            (x.Status == EmailStatus.Initialized && x.CreationTime.AddMinutes(10) < dbNow);

        // Fetch all email logs and apply the filter using LINQ
        var allEmailLogs = await emailLogsRepository.GetListAsync();
        var emailLogs = allEmailLogs.Where(filter.Compile()).ToList();

        // Ensure we're returning 0 if no logs are found
        return emailLogs?.Count ?? 0;
    }

    public async Task<EmailLog?> UpdateEmailLog(Guid emailId, string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
    {
        if (string.IsNullOrEmpty(emailTo))
        {
            return null;
        }
        
        var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom, "html", emailTemplateName);
        EmailLog emailLog = await emailLogsRepository.GetAsync(emailId);
        emailLog = UpdateMappedEmailLog(emailLog, emailObject);
        emailLog.ApplicationId = applicationId;
        emailLog.Id = emailId;
        emailLog.Status = status ?? EmailStatus.Initialized;

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailLog loggedEmail = await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
        return loggedEmail;
    }

    public async Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
    {
        return await InitializeEmailLog(emailTo, body, subject, applicationId, emailFrom, EmailStatus.Initialized, emailTemplateName, emailCC, emailBCC);
    }

    [RemoteService(false)]
    public async Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
    {
        if (string.IsNullOrEmpty(emailTo))
        {
            return null;
        }
        var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom, "html", emailTemplateName, emailCC, emailBCC);
        EmailLog emailLog = new();
        emailLog = UpdateMappedEmailLog(emailLog, emailObject);
        emailLog.ApplicationId = applicationId;
        emailLog.Status = status ?? EmailStatus.Initialized;

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailLog loggedEmail = await emailLogsRepository.InsertAsync(emailLog, autoSave: true);
        return loggedEmail;
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
                    res = await SendEmailNotification(toEmail, htmlBody, subject, fromEmail, "html", input.EmailTemplateName);
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
        try
        {
            if (string.IsNullOrEmpty(emailTo))
            {
                Logger.LogError("EmailNotificationService->SendEmailNotification: The 'emailTo' parameter is null or empty.");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("'emailTo' cannot be null or empty.")
                };

            }
            // Send the email using the CHES client service
            var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom, emailBodyType, emailTemplateName);
            var response = await chesClientService.SendAsync(emailObject);

            // Assuming SendAsync returns a HttpResponseMessage or equivalent:
            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "EmailNotificationService->SendEmailNotification: Exception occurred while sending email.");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent($"An exception occurred while sending the email: {ex.Message}")
            };
        }
    }

    public async Task<EmailLog?> GetEmailLogById(Guid id)
    {
        EmailLog emailLog = new();
        try
        {
            emailLog = await emailLogsRepository.GetAsync(id);
        }
        catch (EntityNotFoundException ex)
        {
            string ExceptionMessage = ex.Message;
            Logger.LogInformation(ex, "Entity Not found for Email Log Must be in wrong context: {ExceptionMessage}", ExceptionMessage);
        }
        return emailLog;
    }

    [Authorize]
    public virtual async Task<List<EmailHistoryDto>> GetHistoryByApplicationId(Guid applicationId)
    {
        var entityList = await emailLogsRepository.GetByApplicationIdAsync(applicationId);
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
    /// <param name="EmailLog">The email log to send to q</param>
    public async Task SendEmailToQueue(EmailLog emailLog)
    {
        EmailNotificationEvent emailNotificationEvent = new()
        {
            Id = emailLog.Id,
            TenantId = emailLog.TenantId,
            RetryAttempts = emailLog.RetryAttempts
        };
        await emailQueueService.SendToEmailEventQueueAsync(emailNotificationEvent);
    }

    protected virtual async Task<dynamic> GetEmailObjectAsync(
                                                string emailTo,
                                                string body,
                                                string subject,
                                                string? emailFrom,
                                                string? emailBodyType,
                                                string? emailTemplateName,
                                                string? emailCC = null,
                                                string? emailBCC = null)
    {
        var toList = emailTo.ParseEmailList() ?? [];
        var ccList = emailCC.ParseEmailList();
        var bccList = emailBCC.ParseEmailList();

        var defaultFromAddress = await SettingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);

        var emailObject = new
        {
            body,
            bodyType = emailBodyType ?? "text",
            cc = ccList,
            bcc = bccList,
            encoding = "utf-8",
            from = emailFrom ?? defaultFromAddress ?? "NoReply@gov.bc.ca",
            priority = "normal",
            subject,
            tag = "tag",
            to = toList,
            templateName = emailTemplateName,
        };
        return emailObject;
    }

    protected virtual EmailLog UpdateMappedEmailLog(EmailLog emailLog, dynamic emailDynamicObject)
    {
        emailLog.Body = emailDynamicObject.body;
        emailLog.Subject = emailDynamicObject.subject;
        emailLog.BodyType = emailDynamicObject.bodyType;
        emailLog.FromAddress = emailDynamicObject.from;
        emailLog.ToAddress = string.Join(",", emailDynamicObject.to);
        emailLog.CC = emailDynamicObject.cc != null ? string.Join(",", (IEnumerable<string>)emailDynamicObject.cc) : string.Empty;
        emailLog.BCC = emailDynamicObject.bcc != null ? string.Join(",", (IEnumerable<string>)emailDynamicObject.bcc) : string.Empty;
        emailLog.TemplateName = emailDynamicObject.templateName;
        return emailLog;
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