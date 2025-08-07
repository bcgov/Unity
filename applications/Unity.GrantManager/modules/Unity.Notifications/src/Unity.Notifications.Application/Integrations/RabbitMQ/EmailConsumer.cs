using System.Threading.Tasks;
using Unity.Notifications.Emails;
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
using Volo.Abp.SettingManagement;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.Settings;

namespace Unity.Notifications.Integrations.RabbitMQ;

public class EmailConsumer(
                    IEmailNotificationService emailNotificationService,
                    IEmailLogsRepository emailLogsRepository,
                    EmailQueueService emailQueueService,
                    IUnitOfWorkManager unitOfWorkManager,
                    ICurrentTenant currentTenant,
                    ISettingManager settingManager,
                    ILogger<EmailConsumer> logger) : IQueueConsumer<EmailMessages>
{
    public async Task<Task> ConsumeAsync(EmailMessages message)
    {
        EmailNotificationEvent emailNotificationEvent = message.EmailNotificationEvent;
        if (emailNotificationEvent == null || emailNotificationEvent.TenantId == Guid.Empty || emailNotificationEvent.Id == Guid.Empty)
        {
            throw new UserFriendlyException("Notification Event null or no Tenant ID or Email Id");
        }

        // Grab the tenant and switch db context
        using (currentTenant.Change(emailNotificationEvent.TenantId))
        {
            var uow = unitOfWorkManager.Begin(true, false);
            EmailLog? emailLog = await emailNotificationService.GetEmailLogById(emailNotificationEvent.Id);
            if (emailLog != null && emailLog.Id != Guid.Empty && emailLog.ToAddress != null)
            {
                await ProcessEmailLogAsync(emailLog, emailNotificationEvent, uow);
            }
        }

        return Task.CompletedTask;
    }

    private async Task ProcessEmailLogAsync(EmailLog emailLog, EmailNotificationEvent emailNotificationEvent, IUnitOfWork uow)
    {
        try
        {
            int maxRetryAttempts = SettingDefinitions.GetSettingsValueInt(settingManager, NotificationsSettings.Mailing.EmailMaxRetryAttempts);

            // Resend the email - Update the RetryCount
            if (emailLog.RetryAttempts <= maxRetryAttempts)
            {
                HttpResponseMessage response = await emailNotificationService.SendEmailNotification(
                                                                                    emailLog.ToAddress,
                                                                                    emailLog.Body,
                                                                                    emailLog.Subject,
                                                                                    emailLog.FromAddress, "html",
                                                                                    emailLog.TemplateName,
                                                                                    emailLog.CC,
                                                                                    emailLog.BCC);
                // Update the response
                emailLog.ChesResponse = JsonConvert.SerializeObject(response);
                emailLog.ChesStatus = response.StatusCode.ToString();

                if (response.StatusCode.ToString() == EmailStatus.Created.ToString())
                {
                    emailLog.Status = EmailStatus.Sent;
                }
                else if (response.StatusCode.ToString() == "0")
                {
                    emailLog.Status = EmailStatus.Failed;
                }

                if (ReprocessBasedOnStatusCode(response.StatusCode))
                {
                    emailLog.RetryAttempts = emailLog.RetryAttempts + 1;
                    await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                    await uow.SaveChangesAsync(); // Timing of Retry update
                    emailNotificationEvent.RetryAttempts = emailLog.RetryAttempts;
                    await emailQueueService.SendToEmailDelayedQueueAsync(emailNotificationEvent);
                }
                else
                {
                    await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                    await uow.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            string ExceptionMessage = ex.Message;
            logger.LogInformation(ex, "Process Delayed Email Exception: {ExceptionMessage}", ExceptionMessage);
        }
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
