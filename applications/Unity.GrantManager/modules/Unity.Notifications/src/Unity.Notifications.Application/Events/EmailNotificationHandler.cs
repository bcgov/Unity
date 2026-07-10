using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Notifications;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Events
{
    internal class EmailNotificationHandler(
            IEmailNotificationService emailNotificationService,
            IFeatureChecker featureChecker,
            EmailAttachmentService emailAttachmentService,
            IEmailLogsRepository emailLogsRepository,
            ICurrentTenant currentTenant,
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<ScheduledNotification, Guid> scheduledNotificationRepository,
            ILoggerFactory loggerFactory) : ILocalEventHandler<EmailNotificationEvent>, ITransientDependency
    {
        private const string FAILED_PAYMENTS_SUBJECT = "CAS Payment Failure Notification";
        private readonly ILogger<EmailNotificationHandler> _logger = loggerFactory.CreateLogger<EmailNotificationHandler>();

        public async Task HandleEventAsync(EmailNotificationEvent eventData)
        {
            if (!await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                return;
            }

            // Switch to the tenant context from the event before processing
            using (currentTenant.Change(eventData.TenantId))
            {
                // Create a new UnitOfWork for this tenant to ensure database operations use the correct tenant's connection
                using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);

                var emailLog = await EmailNotificationEventAsync(eventData);

                await uow.CompleteAsync();

                if (emailLog != null)
                {
                    await emailNotificationService.SendEmailToQueue(emailLog);
                }
            }
        }

        private sealed record EmailInitParams(
            string EmailTo,
            string Body,
            string Subject,
            Guid ApplicationId,
            string? EmailFrom,
            string? EmailTemplateName,
            string? EmailCC = null,
            string? EmailBCC = null,
            DateTime? SendOnDateTime = null);

        private bool HasRecipients(EmailNotificationEvent eventData, string actionName)
        {
            if (eventData.EmailAddressList?.Count > 0)
            {
                return true;
            }
            
            _logger.LogWarning(
                "{Action}: No email addresses provided for Application {ApplicationId}",
                actionName,
                eventData.ApplicationId);
            return false;
        }

        private static (string To, string? Cc, string? Bcc) BuildAddresses(EmailNotificationEvent e)
        {
            return (
                string.Join(",", e.EmailAddressList!),
                e.Cc?.Any() == true ? string.Join(",", e.Cc) : null,
                e.Bcc?.Any() == true ? string.Join(",", e.Bcc) : null
            );
        }

        private static EmailType GetEmailType(EmailAction action, DateTime? sendOnDateTime = null)
        {
            if (sendOnDateTime.HasValue)
            {
                return EmailType.Delayed;
            }

            return action switch
            {
                EmailAction.SendEventDriven => EmailType.EventBased,
                EmailAction.SendDateDriven => EmailType.DateBased,
                _ => EmailType.Manual
            };
        }

        private async Task<EmailLog> InitializeEmailAndUploadAttachments(EmailInitParams p, List<EmailAttachmentData>? emailAttachments = null)
        {
            EmailLog emailLog = await InitializeEmail(p, EmailStatus.Initialized);

            try
            {
                // Upload attachments to S3
                if (emailAttachments != null && emailAttachments.Count != 0)
                {
                    foreach (var attachmentData in emailAttachments)
                    {
                        await emailAttachmentService.UploadAttachmentAsync(
                            emailLog.Id,
                            null, // No templateId for user-uploaded attachments
                            emailLog.TenantId,
                            attachmentData.FileName,
                            attachmentData.Content,
                            attachmentData.ContentType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload email attachments for Email {EmailId}. Email will be sent WITHOUT attachments.", emailLog.Id);
                // DO NOT THROW - email failure should not block calling workflow
                // Email will still be sent after commit, just without attachments
                // even if S3 upload failed - recipients will notice missing attachment
            }

            return emailLog;
        }

        private async Task<EmailLog> InitializeEmail(EmailInitParams p, string status)
        {
            EmailLog emailLog = await emailNotificationService.InitializeEmailLog(
                                                    new EmailMessageParams(p.EmailTo, p.Body, p.Subject,
                                                        p.EmailFrom, p.EmailTemplateName, p.EmailCC, p.EmailBCC, p.SendOnDateTime),
                                                    p.ApplicationId,
                                                    status) ?? throw new UserFriendlyException("Unable to Initialize Email Log");
            return emailLog;
        }

        private async Task<EmailLog?> EmailNotificationEventAsync(EmailNotificationEvent eventData)
        {
            if (eventData == null)
            {
                return null;
            }
            
            return eventData.Action switch
            {
                EmailAction.SendFailedSummary => await HandleFailedSummary(eventData),
                EmailAction.SendCustom or EmailAction.SendEventDriven or EmailAction.SendDateDriven => await HandleSendCustomEmail(eventData),
                EmailAction.SaveDraft => await HandleSaveDraftAndReturnNull(eventData),
                EmailAction.SendFsbNotification => await HandleFsbNotification(eventData),
                _ => null
            };
        }

        private async Task<EmailLog?> HandleFailedSummary(EmailNotificationEvent eventData)
        {
            if (!HasRecipients(eventData, nameof(EmailAction.SendFailedSummary)))
            {
                return null;
            }

            var (to, cc, bcc) = BuildAddresses(eventData);
            var emailLog = await InitializeEmailAndUploadAttachments(
                new EmailInitParams(to, eventData.Body, FAILED_PAYMENTS_SUBJECT,
                    eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName, cc, bcc));
            
            emailLog.Recipient = RecipientType.Internal;
            await StampClassificationAsync(emailLog);
            return emailLog;
        }

        private async Task<EmailLog?> HandleFsbNotification(EmailNotificationEvent eventData)
        {
            if (!HasRecipients(eventData, nameof(EmailAction.SendFsbNotification)))
            {
                return null;
            }

            var (to, cc, bcc) = BuildAddresses(eventData);
            var emailLog = await InitializeEmailAndUploadAttachments(
                new EmailInitParams(to, eventData.Body,
                    eventData.Subject ?? "FSB Payment Notification",
                    eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName, cc, bcc),
                eventData.EmailAttachments);

            if (eventData.PaymentRequestIds?.Count > 0)
            {
                emailLog.PaymentRequestIds = string.Join(",", eventData.PaymentRequestIds);
            }

            emailLog.Recipient = RecipientType.Internal;
            await StampClassificationAsync(emailLog);
            return emailLog;
        }

        private async Task<EmailLog?> HandleSendCustomEmail(EmailNotificationEvent eventData)
        {
            if (!HasRecipients(eventData, nameof(EmailAction.SendCustom)))
            {
                return null;
            }

            var (to, cc, bcc) = BuildAddresses(eventData);
            var messageParams = new EmailMessageParams(
                to, eventData.Body, eventData.Subject,
                eventData.EmailFrom, eventData.EmailTemplateName, cc, bcc, eventData.SendOnDateTime);

            var emailLog = await CreateOrUpdateEmail(eventData.Id, messageParams, eventData.ApplicationId, eventData.EmailAttachments, EmailStatus.Initialized);
            
            if (emailLog == null)
            {
                return null;
            }

            emailLog.EmailType = GetEmailType(eventData.Action);
            
            if (eventData.ScheduledNotificationId.HasValue && !emailLog.ScheduledNotificationId.HasValue)
            {
                emailLog.ScheduledNotificationId = eventData.ScheduledNotificationId.Value;
                await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
            }
            
            await StampClassificationAsync(emailLog);
            return emailLog;
        }

        private async Task<EmailLog?> CreateOrUpdateEmail(
            Guid emailId,
            EmailMessageParams messageParams,
            Guid applicationId,
            List<EmailAttachmentData>? attachments,
            string status)
        {
            if (emailId == Guid.Empty)
            {
                return await InitializeEmailAndUploadAttachments(
                    new EmailInitParams(
                        messageParams.EmailTo,
                        messageParams.Body,
                        messageParams.Subject,
                        applicationId,
                        messageParams.EmailFrom,
                        messageParams.EmailTemplateName,
                        messageParams.EmailCC,
                        messageParams.EmailBCC,
                        messageParams.SendOnDateTime),
                    attachments);
            }

            var existingEmail = await emailLogsRepository.FindAsync(emailId);
            if (existingEmail != null)
            {
                return await emailNotificationService.UpdateEmailLog(
                    emailId,
                    messageParams,
                    applicationId,
                    status);
            }
            
            return await InitializeEmailAndUploadAttachments(
                new EmailInitParams(
                    messageParams.EmailTo,
                    messageParams.Body,
                    messageParams.Subject,
                    applicationId,
                    messageParams.EmailFrom,
                    messageParams.EmailTemplateName,
                    messageParams.EmailCC,
                    messageParams.EmailBCC,
                    messageParams.SendOnDateTime),
                attachments);
        }

        private async Task<EmailLog> StampClassificationAsync(EmailLog emailLog)
        {
            emailLog.Recipient = RecipientType.External;
            
            if (emailLog.ScheduledNotificationId.HasValue)
            {
                var notification = await scheduledNotificationRepository.FindAsync(emailLog.ScheduledNotificationId.Value);
                if (notification?.RecipientCategory?.Equals("Internal", StringComparison.OrdinalIgnoreCase) == true)
                {
                    emailLog.Recipient = RecipientType.Internal;
                }
            }
            else if (emailLog.Recipient == RecipientType.Internal)
            {
                // Preserve explicitly set Internal recipient
            }
            else
            {
                emailLog.Recipient = RecipientType.External;
            }
            
            await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
            return emailLog;
        }

        private async Task<EmailLog?> HandleSaveDraftAndReturnNull(EmailNotificationEvent eventData)
        {
            if (!HasRecipients(eventData, nameof(EmailAction.SaveDraft)))
            {
                return null;
            }

            var (to, cc, bcc) = BuildAddresses(eventData);
            var messageParams = new EmailMessageParams(
                to, eventData.Body, eventData.Subject,
                eventData.EmailFrom, eventData.EmailTemplateName, cc, bcc, eventData.SendOnDateTime);

            var emailLog = await CreateOrUpdateEmail(eventData.Id, messageParams, eventData.ApplicationId, null, EmailStatus.Draft);
            
            if (emailLog != null)
            {
                await StampClassificationAsync(emailLog);
            }
            
            return null;
        }
    }
}

