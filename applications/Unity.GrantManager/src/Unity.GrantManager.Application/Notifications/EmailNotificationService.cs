using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Integration.Ches;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Notifications;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(EmailNotificationService), typeof(IEmailNotificationService))]
public class EmailNotificationService : ApplicationService, IEmailNotificationService
{
    private readonly IApplicantAgentRepository _applicantAgentRepository;
    private readonly IChesClientService _chesClientService;
    private readonly IConfiguration _configuration;

    public EmailNotificationService(
        IApplicantAgentRepository applicantAgentRepository,
        IConfiguration configuration,
        IChesClientService chesClientService
        )
    {
        _applicantAgentRepository = applicantAgentRepository;
        _configuration = configuration;
        _chesClientService = chesClientService;
    }

    private const string approvalBody = @"Hello,<br>
    Thank you for your grant application.We are pleased to inform you that your project has been approved for funding.<br>
    Next, you will hear from a program analyst who will send you more information about the funding, including a Contribution Agreement which will outline the terms, payment schedule and reporting requirements.<br>
    Kind regards.";

    private const string declineBody = @"Hello,<br>
    Thank you for your application. We would like to advise you that, after careful consideration, your project was not selected to receive funding.<br>
    We know that a lot of effort goes into developing a proposed project and we appreciate the time you took to prepare your application.<br>
    Program staff are available to provide further details regarding the funding decision.<br>
    Thank you again for your application.";

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
    /// <param name="id">The application</param>
    /// <param name="body">The body of the email</param>
    /// <param name="subject">Subject Message</param>
    public async Task SendEmailNotification(Guid id, string body, string subject)
    {
        try {
            // Lookup the applicant email
            var applicantAgent = await _applicantAgentRepository.FirstOrDefaultAsync(a => a.ApplicationId == id) ?? throw new EntityNotFoundException();
            if(!string.IsNullOrEmpty(applicantAgent.Email))
            {
                List<string> toList = [applicantAgent.Email];
                var emailObject = new
                {
                    body = body,
                    bodyType = "html",
                    encoding = "utf-8",
                    from = _configuration["Notifications:ChesFromEmail"] ?? "unity@gov.bc.ca",
                    priority = "normal",
                    subject = subject,
                    tag = "tag",
                    to = toList
                };

                await _chesClientService.SendAsync(emailObject);
            } else
            {
                Logger.LogError("EmailNotificationService->SendEmailNotification: No Applicant Agent Email Found.");
            }
        } catch (Exception ex) {
            Logger.LogError("EmailNotificationService->SendEmailNotification Exception: {message}", ex.Message);
        }
    }
}