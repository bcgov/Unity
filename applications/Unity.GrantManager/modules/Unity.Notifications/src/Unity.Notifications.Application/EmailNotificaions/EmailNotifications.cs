using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EmailNotificaions;
using Unity.Notifications.Emails;
using Unity.Notifications.Integrations.Ches;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.Notifications.EmailNotifications;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(EmailNotificationService), typeof(IEmailNotificationService))]
public class EmailNotificationService : ApplicationService, IEmailNotificationService
{
    private readonly IChesClientService _chesClientService;
    private readonly IConfiguration _configuration;
    private readonly EmailQueueService _emailQueueService;
    private readonly IEmailLogsRepository _emailLogsRepository;

    public EmailNotificationService(
        IEmailLogsRepository emailLogsRepository,
        IConfiguration configuration,
        IChesClientService chesClientService,
        EmailQueueService emailQueueService
        )
    {
        _emailLogsRepository = emailLogsRepository;
        _configuration = configuration;
        _chesClientService = chesClientService;
        _emailQueueService = emailQueueService;
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
    /// <param name="email">The email address to send to</param>
    /// <param name="body">The body of the email</param>
    /// <param name="subject">Subject Message</param>
    public async Task<RestResponse> SendEmailNotification(string email, string body, string subject, Guid applicationId)
    {
        RestResponse response = new RestResponse();
        try
        {
            if (!string.IsNullOrEmpty(email))
            {
                var emailObject = GetEmailObject(email, body, subject, applicationId);
                response = await _chesClientService.SendAsync(emailObject);
                await LogEmailResponse(emailObject, response);
            }
            else
            {
                Logger.LogError("EmailNotificationService->SendEmailNotification: Email To Found.");
            }
        }
        catch (Exception ex)
        {
            string exceptionMessage = ex.Message;
            Logger.LogError(ex, "EmailNotificationService->SendEmailNotification Exception: {exceptionMessage}", exceptionMessage);
        }
        return response;
    }

    /// <summary>
    /// Send Email To Queue
    /// </summary>
    /// <param name="email">The email address to send to</param>
    /// <param name="body">The body of the email</param>
    /// <param name="subject">Subject Message</param>
    public async Task SendEmaiToQueue(string emailTo, string body, string subject, Guid applicationId)
    {
        if (!string.IsNullOrEmpty(emailTo))
        {            
            var emailObject = GetEmailObject(emailTo, body, subject, applicationId);
            EmailLog emailLog = GetMappedEmailLog(emailObject);            
            EmailLog loggedEmail = await _emailLogsRepository.InsertAsync(emailLog, autoSave: true);
            await _emailQueueService.SendToEmailQueueAsync(loggedEmail);
        }
    }

    protected virtual dynamic GetEmailObject(string emailTo, string body, string subject, Guid applicationId)
    {
        List<string> toList = new() { emailTo };
        var emailObject = new
        {
            body,
            bodyType = "html",
            encoding = "utf-8",
            from = _configuration["Notifications:ChesFromEmail"] ?? "unity@gov.bc.ca",
            priority = "normal",
            subject,
            tag = "tag",
            to = toList,
            applicationId
        };
        return emailObject;
    }

    protected virtual async Task<EmailLog> LogEmailResponse(object emailObject, RestResponse response)
    {
        EmailLog emailLog = GetMappedEmailLog(emailObject);
        emailLog.ChesResponse = JsonConvert.SerializeObject(response);
        emailLog.ChesStatus = response.StatusCode.ToString();
        return await _emailLogsRepository.InsertAsync(emailLog, autoSave: true);
    }

    protected virtual EmailLog GetMappedEmailLog(dynamic emailDynamicObject)
    {
        EmailLog emailLog = new EmailLog();
        emailLog.Body = emailDynamicObject.body;
        emailLog.Subject = emailDynamicObject.subject;
        emailLog.BodyType = emailDynamicObject.bodyType;
        emailLog.FromAddress = emailDynamicObject.from;
        emailLog.ToAddress = ((List<string>)emailDynamicObject.to).FirstOrDefault() ?? "";
        emailLog.ApplicantId = emailDynamicObject.applicationId;
        return emailLog;
    }
}