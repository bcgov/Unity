using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Unity.Modules.Shared.MessageBrokers.RabbitMQ.Interfaces;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Events;
using Unity.Notifications.Integrations.RabbitMQ.QueueMessages;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Unity.Notifications.Emails;

namespace Unity.Notifications.Integrations.RabbitMQ;

public class EmailConsumer(
    IEmailNotificationService emailNotificationService,
    IEmailLogsRepository emailLogsRepository,
    EmailQueueService emailQueueService,
    IUnitOfWorkManager unitOfWorkManager,
    ICurrentTenant currentTenant,
    ILogger<EmailConsumer> logger
) : IQueueConsumer<EmailMessages>
{
    const int maxRetries = 10;

    // -----------------------------
    //      PUBLIC ENTRY POINT
    // -----------------------------
    public async Task ConsumeAsync(EmailMessages message)
    {
        var notificationEvent = message.EmailNotificationEvent;

        ValidateMessage(notificationEvent);

        using (currentTenant.Change(notificationEvent.TenantId))
        {
            using var uow = unitOfWorkManager.Begin(requiresNew: true);

            var emailLog = await emailNotificationService.GetEmailLogById(notificationEvent.Id);

            if (emailLog == null || !ShouldProcessEmail(emailLog))
            {
                logger.LogInformation(
                    "Email {EmailId} already processed or not found. Tenant {TenantId}. Skipping.",
                    notificationEvent.Id,
                    notificationEvent.TenantId
                );
                return;
            }

            await ProcessEmailAsync(emailLog, notificationEvent, uow);

            await uow.CompleteAsync();
        }
    }

    // -----------------------------
    //      CORE PROCESSING
    // -----------------------------
    private async Task ProcessEmailAsync(
        EmailLog emailLog,
        EmailNotificationEvent notificationEvent,
        IUnitOfWork uow)
    {
        try
        {          
            if (emailLog.RetryAttempts > maxRetries)
            {
                logger.LogWarning(
                    "Email {EmailId} exceeded max retry attempts ({Attempts}).",
                    emailLog.Id,
                    maxRetries
                );

                emailLog.Status = EmailStatus.Failed;
                await SaveEmailLogWithRetryAsync(emailLog, uow);
                return;
            }

            HttpResponseMessage response;

            try
            {
                response = await emailNotificationService.SendEmailNotification(
                    emailLog.ToAddress,
                    emailLog.Body,
                    emailLog.Subject,
                    emailLog.FromAddress,
                    "html",
                    emailLog.TemplateName,
                    emailLog.CC,
                    emailLog.BCC
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Email sending failed for Email {EmailId}. Tenant {TenantId}. Marking as failure and retrying.",
                    emailLog.Id,
                    notificationEvent.TenantId
                );

                emailLog.Status = EmailStatus.Failed;
                await HandleRetryAsync(emailLog, notificationEvent, uow);
                return;
            }

            UpdateEmailLogStatus(emailLog, response);

            if (ShouldRetry(response.StatusCode))
            {
                await HandleRetryAsync(emailLog, notificationEvent, uow);
            }
            else
            {
                await SaveEmailLogWithRetryAsync(emailLog, uow);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected processing error for Email {EmailId}.",
                emailLog.Id
            );
        }
    }

    // -----------------------------
    //      RETRY HANDLING
    // -----------------------------
    private async Task HandleRetryAsync(
        EmailLog log,
        EmailNotificationEvent notificationEvent,
        IUnitOfWork uow)
    {
        log.RetryAttempts++;
        await SaveEmailLogWithRetryAsync(log, uow);

        notificationEvent.RetryAttempts = log.RetryAttempts;

        await emailQueueService.SendToEmailDelayedQueueAsync(notificationEvent);

        logger.LogWarning(
            "Retry scheduled for Email {EmailId}. Attempt {RetryAttempts}.",
            log.Id,
            log.RetryAttempts
        );
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        new[]
        {
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        }.Contains(statusCode);

    // -----------------------------
    //      STATUS UPDATE
    // -----------------------------
    private void UpdateEmailLogStatus(EmailLog log, HttpResponseMessage response)
    {
        log.ChesResponse = JsonConvert.SerializeObject(new
        {
            response.StatusCode,
            Headers = response.Headers?.ToString(),
            Body = response.Content != null ? response.Content.ReadAsStringAsync().Result : null
        });

        log.ChesStatus = response.StatusCode.ToString();

        log.Status = response.IsSuccessStatusCode
            ? EmailStatus.Sent
            : EmailStatus.Failed;
    }

    // -----------------------------
    //      SAFE-CONCURRENCY SAVE
    // -----------------------------
    private async Task SaveEmailLogWithRetryAsync(
        EmailLog emailLog,
        IUnitOfWork uow,
        int maxRetries = 3)
    {
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                await emailLogsRepository.UpdateAsync(emailLog, autoSave: false);
                await uow.SaveChangesAsync();
                return;
            }
            catch (Exception ex) when (
                ex is AbpDbConcurrencyException ||
                ex is DbUpdateConcurrencyException
            )
            {
                attempt++;

                if (attempt >= maxRetries)
                {
                    logger.LogError(
                        ex,
                        "Max concurrency retries reached for EmailLog {EmailId}. Manual intervention required.",
                        emailLog.Id
                    );
                    throw;
                }

                logger.LogWarning(
                    ex,
                    "Concurrency conflict on EmailLog {EmailId}, retry {Attempt}. Reloading entity.",
                    emailLog.Id,
                    attempt
                );

                await Task.Delay(100);

                var fresh = await emailNotificationService.GetEmailLogById(emailLog.Id);
                if (fresh != null)
                {
                    emailLog.ConcurrencyStamp = fresh.ConcurrencyStamp;
                }
            }
        }
    }

    // -----------------------------
    //      VALIDATION / HELPERS
    // -----------------------------
    private static void ValidateMessage(EmailNotificationEvent evt)
    {
        if (evt == null ||
            evt.TenantId == Guid.Empty ||
            evt.Id == Guid.Empty)
        {
            throw new UserFriendlyException("Notification event is missing required identifiers.");
        }
    }

    private static bool ShouldProcessEmail(EmailLog log)
        => log != null && log.Status != EmailStatus.Sent;
}
