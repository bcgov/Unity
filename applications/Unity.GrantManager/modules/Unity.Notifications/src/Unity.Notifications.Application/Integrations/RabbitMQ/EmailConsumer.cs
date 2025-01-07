using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Microsoft.Extensions.Options;
using System;
using Volo.Abp.Uow;
using System.Net.Http;
using System.Net;
using System.Linq;
using Unity.Notifications.Events;
using Volo.Abp.MultiTenancy;
using Volo.Abp;
using Unity.Notifications.Integrations.RabbitMQ.QueueMessages;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Microsoft.Extensions.Logging;
using Unity.Notifications.EmailNotifications;
using Newtonsoft.Json;

namespace Unity.Notifications.Integrations.RabbitMQ;

public class EmailConsumer : IQueueConsumer<EmailMessages>
{
    private int _retryAttemptMax { get; set; } = 0;
    private readonly EmailQueueService _emailQueueService;
    private readonly IEmailLogsRepository _emailLogsRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<EmailConsumer> _logger;
    private readonly IEmailNotificationService _emailNotificationService;

    public EmailConsumer(
                    IEmailNotificationService emailNotificationService,
                    IEmailLogsRepository emailLogsRepository,
                    IOptions<EmailBackgroundJobsOptions> emailBackgroundJobsOptions,
                    EmailQueueService emailQueueService,
                    IUnitOfWorkManager unitOfWorkManager,
                    ICurrentTenant currentTenant,
                    ILogger<EmailConsumer> logger
                    )
    {
        _emailNotificationService = emailNotificationService;
        _emailQueueService = emailQueueService;
        _retryAttemptMax = emailBackgroundJobsOptions.Value.EmailResend.RetryAttemptsMaximum;
        _emailLogsRepository = emailLogsRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<Task> ConsumeAsync(EmailMessages message)
    {
        EmailNotificationEvent emailNotificationEvent = message.EmailNotificationEvent;
        if (emailNotificationEvent == null || emailNotificationEvent.TenantId == Guid.Empty || emailNotificationEvent.Id == Guid.Empty)
        {
            throw new UserFriendlyException("Notification Event null or no Tenant ID or Email Id");
        }

        // Grab the tenant and switch db context
        using (_currentTenant.Change(emailNotificationEvent.TenantId))
        {
            var uow = _unitOfWorkManager.Begin(true, false);
            EmailLog? emailLog = await _emailNotificationService.GetEmailLogById(emailNotificationEvent.Id);
            if (emailLog != null && emailLog.Id != Guid.Empty && emailLog.ToAddress != null)
            {

                try
                {
                    // Resend the email - Update the RetryCount
                    if (emailLog.RetryAttempts <= _retryAttemptMax)
                    {
                        HttpResponseMessage response = await _emailNotificationService.SendEmailNotification(
                                                                                        emailLog.ToAddress,
                                                                                        emailLog.Body,
                                                                                        emailLog.Subject,
                                                                                        emailLog.FromAddress);
                        // Update the response
                        emailLog.ChesResponse = JsonConvert.SerializeObject(response);
                        emailLog.ChesStatus = response.StatusCode.ToString();

                        if(response.StatusCode.ToString() == EmailStatus.Sent) {
                            emailLog.Status = EmailStatus.Sent;
                        } else if (response.StatusCode.ToString() == "0") {
                            emailLog.Status = EmailStatus.Failed;
                        }

                        if (ReprocessBasedOnStatusCode(response.StatusCode))
                        {
                            emailLog.RetryAttempts = emailLog.RetryAttempts + 1;
                            await _emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                            await uow.SaveChangesAsync(); // Timing of Retry update
                            emailNotificationEvent.RetryAttempts = emailLog.RetryAttempts;
                            await _emailQueueService.SendToEmailDelayedQueueAsync(emailNotificationEvent);
                        } else {
                            await _emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                            await uow.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    string ExceptionMessage = ex.Message;
                    _logger.LogInformation(ex, "Process Delayed Email Exception: {ExceptionMessage}", ExceptionMessage);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool ReprocessBasedOnStatusCode(HttpStatusCode statusCode)
    {
        HttpStatusCode[] reprocessStatusCodes = {
             HttpStatusCode.TooManyRequests,
             HttpStatusCode.InternalServerError,
             HttpStatusCode.BadGateway,
             HttpStatusCode.ServiceUnavailable,
             HttpStatusCode.GatewayTimeout,
        };

        return reprocessStatusCodes.Contains(statusCode);
    }
}
