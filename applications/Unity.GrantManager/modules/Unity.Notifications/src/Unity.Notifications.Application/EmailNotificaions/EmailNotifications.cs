using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Notifications.Integrations.Ches;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.Notifications.EmailNotifications;

[RequiresFeature("Unity.Notifications")]
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(EmailNotificationService), typeof(IEmailNotificationService))]
public class EmailNotificationService : ApplicationService, IEmailNotificationService
{
    private readonly IChesClientService _chesClientService;
    private readonly IConfiguration _configuration;
    private RegistrationService _registrationService;

    public EmailNotificationService(
        IConfiguration configuration,
        IChesClientService chesClientService,
        RegistrationService registrationService
        )
    {
        _configuration = configuration;
        _chesClientService = chesClientService;
        _registrationService = registrationService;
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
        Program area staff are available to provide further details regarding the funding decision.<br>
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

    /// <summary>
    /// Send Email Notfication
    /// </summary>
    /// <param name="email">The email address to send to</param>
    /// <param name="body">The body of the email</param>
    /// <param name="subject">Subject Message</param>
    public async Task SendEmailNotification(string email, string body, string subject)
    {
        try
        {
            // If the CHES service is not available - q it
            //var chesHealthResponse = await _chesClientService.HealthCheckAsync();
            //if( chesHealthResponse != null )
            //{
                _registrationService.RegisterAsync(email, subject, body);
            //}


            if (!string.IsNullOrEmpty(email))
            {
                List<string> toList = new() { email };
                var emailObject = new
                {
                    body,
                    bodyType = "html",
                    encoding = "utf-8",
                    from = _configuration["Notifications:ChesFromEmail"] ?? "unity@gov.bc.ca",
                    priority = "normal",
                    subject,
                    tag = "tag",
                    to = toList
                };

                await _chesClientService.SendAsync(emailObject);
            }
            else
            {
                Logger.LogError("EmailNotificationService->SendEmailNotification: No Applicant Agent Email Found.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("EmailNotificationService->SendEmailNotification Exception: {message}", ex.Message);
        }
    }
}