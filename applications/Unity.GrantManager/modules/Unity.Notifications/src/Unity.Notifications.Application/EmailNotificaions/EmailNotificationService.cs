using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Integrations.Ches;
using Unity.Notifications.Integrations.RabbitMQ;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Users;
using Volo.Abp.SettingManagement;
using Unity.Notifications.Settings;
using Unity.Notifications.Permissions;
using Volo.Abp;

namespace Unity.Notifications.EmailNotifications;


[Dependency(ReplaceServices = false)]
[ExposeServices(typeof(EmailNotificationService), typeof(IEmailNotificationService))]
public class EmailNotificationService : ApplicationService, IEmailNotificationService
{
    private readonly IChesClientService _chesClientService;
    private readonly IConfiguration _configuration;
    private readonly EmailQueueService _emailQueueService;
    private readonly IEmailLogsRepository _emailLogsRepository;
    private readonly IExternalUserLookupServiceProvider _externalUserLookupServiceProvider;
    private readonly ISettingManager _settingManager;

    public EmailNotificationService(
        IEmailLogsRepository emailLogsRepository,
        IConfiguration configuration,
        IChesClientService chesClientService,
        EmailQueueService emailQueueService,
        IExternalUserLookupServiceProvider externalUserLookupServiceProvider,
        ISettingManager settingManager
        )
    {
        _emailLogsRepository = emailLogsRepository;
        _configuration = configuration;
        _chesClientService = chesClientService;
        _emailQueueService = emailQueueService;
        _externalUserLookupServiceProvider = externalUserLookupServiceProvider;
        _settingManager = settingManager;
    }

    private const string approvalBody =
        @"Hello,<br>
        <br>
        Thank you for your grant application. We are pleased to inform you that your project has been approved for funding.<br>
        A representative from our Program Area will be reaching out to you shortly with more information on next steps.<br>
        <br>
        Kind regards.<br>
        <br>
        *ATTENTION - Please do not reply to this email as it is an automated notification which is unable to receive replies.<br>";

    private const string declineBody =
        @"Hello,<br>
        <br>
        Thank you for your application. We would like to advise you that after careful consideration, your project was not selected to receive funding from our Program.<br>
        <br>
        We know that a lot of effort goes into developing a proposed project and we appreciate the time you took to prepare your application.<br>
        <br>
        If you have any questions or concerns, please reach out to program team members who will provide further details regarding the funding decision.<br>
        <br>
        Thank you again for your application.<br>
        <br>
        *ATTENTION - Please do not reply to this email as it is an automated notification which is unable to receive replies.<br>";

    public string GetApprovalBody()
    {
        return approvalBody;
    }

    public string GetDeclineBody()
    {
        return declineBody;
    }

    public async Task DeleteEmail(Guid id)
    {
        await _emailLogsRepository.DeleteAsync(id);
    }

    public async Task<EmailLog?> UpdateEmailLog(Guid emailId, string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status)
    {
        if (string.IsNullOrEmpty(emailTo))
        {
            return null;
        }
        
        var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom);
        EmailLog emailLog = await _emailLogsRepository.GetAsync(emailId);
        emailLog = UpdateMappedEmailLog(emailLog, emailObject);
        emailLog.ApplicationId = applicationId;
        emailLog.Id = emailId;
        emailLog.Status = status ?? EmailStatus.Initialized;

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailLog loggedEmail = await _emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
        return loggedEmail;
    }

    public async Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom)
    {
        return await InitializeEmailLog(emailTo, body, subject, applicationId, emailFrom, EmailStatus.Initialized);
    }

    [RemoteService(false)]
    public async Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status)
    {
        if (string.IsNullOrEmpty(emailTo))
        {
            return null;
        }
        var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom);
        EmailLog emailLog = new EmailLog();
        emailLog = UpdateMappedEmailLog(emailLog, emailObject);
        emailLog.ApplicationId = applicationId;
        emailLog.Status = status ?? EmailStatus.Initialized;

        // When being called here the current tenant is in context - verified by looking at the tenant id
        EmailLog loggedEmail = await _emailLogsRepository.InsertAsync(emailLog, autoSave: true);
        return loggedEmail;
    }

    protected virtual async Task NotifyTeamsChannel(string chesEmailError)
    {
        string? envInfo = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string activityTitle = "CHES Email error: " + chesEmailError;
        string activitySubtitle = "Environment: " + envInfo;
        string teamsChannel = _configuration["Notifications:TeamsNotificationsWebhook"] ?? "";
        List<Fact> facts = new() { };
        await TeamsNotificationService.PostToTeamsAsync(teamsChannel, activityTitle, activitySubtitle, facts);
    }

    /// <summary>
    /// Send Email Notfication
    /// </summary>
    /// <param name="emailTo">The email address to send to</param>
    /// <param name="body">The body of the email</param>
    /// <param name="subject">Subject Message</param>
    /// <param name="emailFrom">From Email Address</param>
    /// <returns>HttpResponseMessage indicating the result of the operation</returns>
    public async Task<HttpResponseMessage> SendEmailNotification(string emailTo, string body, string subject, string? emailFrom)
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
            var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom);
            var response = await _chesClientService.SendAsync(emailObject);

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
        EmailLog emailLog = new EmailLog();
        try
        {
            emailLog = await _emailLogsRepository.GetAsync(id);
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
        var entityList = await _emailLogsRepository.GetByApplicationIdAsync(applicationId);
        var dtoList = ObjectMapper.Map<List<EmailLog>, List<EmailHistoryDto>>(entityList);

        var sentByUserIds = dtoList
            .Where(d => d.CreatorId.HasValue)
            .Select(d => d.CreatorId!.Value)
            .Distinct();

        var userDictionary = new Dictionary<Guid, EmailHistoryUserDto>();

        foreach (var userId in sentByUserIds)
        {
            var userInfo = await _externalUserLookupServiceProvider.FindByIdAsync(userId);
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
        EmailNotificationEvent emailNotificationEvent = new EmailNotificationEvent();
        emailNotificationEvent.Id = emailLog.Id;
        emailNotificationEvent.TenantId = emailLog.TenantId;
        emailNotificationEvent.RetryAttempts = emailLog.RetryAttempts;
        await _emailQueueService.SendToEmailEventQueueAsync(emailNotificationEvent);
    }

    protected virtual async Task<dynamic> GetEmailObjectAsync(string emailTo, string body, string subject, string? emailFrom)
    {
        List<string> toList = new();
        string[] emails = emailTo.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);

        foreach (string email in emails)
        {
            toList.Add(email.Trim());
        }

        var defaultFromAddress = await SettingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);

        var emailObject = new
        {
            body,
            bodyType = "text",
            encoding = "utf-8",
            from = emailFrom ?? defaultFromAddress ?? "NoReply@gov.bc.ca",
            priority = "normal",
            subject,
            tag = "tag",
            to = toList
        };
        return emailObject;
    }

    protected virtual EmailLog UpdateMappedEmailLog(EmailLog emailLog, dynamic emailDynamicObject)
    {
        emailLog.Body = emailDynamicObject.body;
        emailLog.Subject = emailDynamicObject.subject;
        emailLog.BodyType = emailDynamicObject.bodyType;
        emailLog.FromAddress = emailDynamicObject.from;
        emailLog.ToAddress = ((List<string>)emailDynamicObject.to).FirstOrDefault() ?? "";
        return emailLog;
    }

    [Authorize(NotificationsPermissions.Settings)]
    public async Task UpdateSettings(NotificationsSettingsDto settingsDto)
    {
        await UpdateTenantSettings(NotificationsSettings.Mailing.DefaultFromAddress, settingsDto.DefaultFromAddress);
        await UpdateTenantSettings(NotificationsSettings.Mailing.EmailMaxRetryAttempts, settingsDto.MaximumRetryAttempts);
    }

    private async Task UpdateTenantSettings(string settingKey, string valueString) {
        if (!valueString.IsNullOrWhiteSpace())
        {
            await _settingManager.SetForCurrentTenantAsync(settingKey, valueString);
        }
    }
}