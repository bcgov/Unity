using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
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
            ILogger<EmailNotificationHandler> logger) : ILocalEventHandler<EmailNotificationEvent>, ITransientDependency
    {
        private const string FAILED_PAYMENTS_SUBJECT = "CAS Payment Failure Notification";

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
                            emailLog.TenantId,
                            attachmentData.FileName,
                            attachmentData.Content,
                            attachmentData.ContentType);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to upload email attachments for Email {EmailId}. Email will be sent WITHOUT attachments.", emailLog.Id);
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
            
            switch (eventData.Action)
            {
                case EmailAction.SendFailedSummary:
                {
                    if (eventData.EmailAddressList == null || eventData.EmailAddressList.Count == 0)
                    {
                        logger.LogWarning("SendFailedSummary: No email addresses provided for Application {ApplicationId}", eventData.ApplicationId);
                        return null;
                    }

                    string emailToAddress = String.Join(",", eventData.EmailAddressList);

<<<<<<< HEAD
                    return await InitializeEmailAndUploadAttachments(
                        new EmailInitParams(emailToAddress, eventData.Body, FAILED_PAYMENTS_SUBJECT,
                            eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName));
                }
                case EmailAction.SendCustom:
                    return await HandleSendCustomEmail(eventData);

                case EmailAction.SaveDraft:
                    await HandleSaveDraftEmail(eventData);
                    return null;

                case EmailAction.SendFsbNotification:
                {
                    if (eventData.EmailAddressList == null || eventData.EmailAddressList.Count == 0)
                    {
                        logger.LogWarning("SendFsbNotification: No email addresses provided for Application {ApplicationId}", eventData.ApplicationId);
                        return null;
                    }

                    string fsbEmailToAddress = String.Join(",", eventData.EmailAddressList);
                    var emailLog = await InitializeEmailAndUploadAttachments(
                        new EmailInitParams(fsbEmailToAddress, eventData.Body,
                            eventData.Subject ?? "FSB Payment Notification",
                            eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName),
                        eventData.EmailAttachments);

                    // Store payment request IDs for tracking
                    if (eventData.PaymentRequestIds != null && eventData.PaymentRequestIds.Count != 0)
                    {
                        emailLog.PaymentRequestIds = string.Join(",", eventData.PaymentRequestIds);
                    }

                    return await StampClassificationAsync(emailLog, EmailType.EventBased, RecipientType.Internal);
                }
                case EmailAction.Retry:
                default:
                    return null;
            }
        }

        private async Task<EmailLog?> HandleSendCustomEmail(EmailNotificationEvent eventData)
        {
            if (eventData.EmailAddressList == null || eventData.EmailAddressList.Count == 0)
            {
                logger.LogWarning("SendCustom: No email addresses provided for Application {ApplicationId}", eventData.ApplicationId);
                return null;
            }

            string emailToAddress = String.Join(",", eventData.EmailAddressList);
            string? emailCC = eventData.Cc?.Any() == true ? String.Join(",", eventData.Cc) : null;
            string? emailBCC = eventData.Bcc?.Any() == true ? String.Join(",", eventData.Bcc) : null;

<<<<<<< HEAD
            EmailLog? emailLog;
            if (eventData.Id == Guid.Empty)
            {
                emailLog = await InitializeEmailAndUploadAttachments(
                    new EmailInitParams(emailToAddress, eventData.Body, eventData.Subject,
                        eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName,
                        emailCC, emailBCC, eventData.SendOnDateTime),
                    eventData.EmailAttachments);
                
                // Set ScheduledNotificationId if provided
                if (emailLog != null && eventData.ScheduledNotificationId.HasValue)
                {
                    emailLog.ScheduledNotificationId = eventData.ScheduledNotificationId.Value;
                    await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                }
            }
            else
            {
                // Check if email exists first
                var existingEmail = await emailLogsRepository.FindAsync(eventData.Id);
                
                if (existingEmail == null)
                {
                    // Email doesn't exist, create new one instead
                    logger.LogWarning(
                        "SendCustom: Email {EmailId} not found for Application {ApplicationId}, creating new email instead.",
                        eventData.Id, eventData.ApplicationId);
                    
                    emailLog = await InitializeEmailAndUploadAttachments(
                        new EmailInitParams(emailToAddress, eventData.Body, eventData.Subject,
                            eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName,
                            emailCC, emailBCC, eventData.SendOnDateTime),
                        eventData.EmailAttachments);
                    
                    // Set ScheduledNotificationId if provided
                    if (emailLog != null && eventData.ScheduledNotificationId.HasValue)
                    {
                        emailLog.ScheduledNotificationId = eventData.ScheduledNotificationId.Value;
                        await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                    }
                }
                else
                {
                    // Email exists, update it
                    var emailMessageParams = new EmailMessageParams(emailToAddress, eventData.Body, eventData.Subject,
                            eventData.EmailFrom, eventData.EmailTemplateName, emailCC, emailBCC, eventData.SendOnDateTime);
                            
                    emailLog = await emailNotificationService.UpdateEmailLog(
                        eventData.Id,
                        emailMessageParams,
                        eventData.ApplicationId,
                        EmailStatus.Initialized) ?? throw new UserFriendlyException("Unable to update Email Log");
                    
                    // Set ScheduledNotificationId if provided and not already set
                    if (emailLog != null && eventData.ScheduledNotificationId.HasValue && !emailLog.ScheduledNotificationId.HasValue)
                    {
                        emailLog.ScheduledNotificationId = eventData.ScheduledNotificationId.Value;
                        await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                    }
                }
            }

            return emailLog;
        }

        private async Task<EmailLog> StampClassificationAsync(EmailLog emailLog, EmailType emailType, RecipientType recipient)
        {
            emailLog.EmailType = emailType;
            emailLog.Recipient = recipient;
            await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
            return emailLog;
        }

        private async Task HandleSaveDraftEmail(EmailNotificationEvent eventData)
        {
                if (eventData.EmailAddressList == null || eventData.EmailAddressList.Count == 0)
                {
                    logger.LogWarning("SaveDraft: No email addresses provided for Application {ApplicationId}", eventData.ApplicationId);
                    return;
                }

                string emailToAddress = String.Join(",", eventData.EmailAddressList);
                string? emailCC = eventData.Cc?.Any() == true ? String.Join(",", eventData.Cc) : null;
                string? emailBCC = eventData.Bcc?.Any() == true ? String.Join(",", eventData.Bcc) : null;

                if (eventData.Id != Guid.Empty)
                {
                    await emailNotificationService.UpdateEmailLog(
                        eventData.Id,
                        new EmailMessageParams(emailToAddress, eventData.Body, eventData.Subject,
                            eventData.EmailFrom, eventData.EmailTemplateName, emailCC, emailBCC, eventData.SendOnDateTime),
                        eventData.ApplicationId,
                        EmailStatus.Draft);
                }
                else
                {
                    await InitializeEmail(
                        new EmailInitParams(emailToAddress, eventData.Body, eventData.Subject,
                            eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName,
                            emailCC, emailBCC, eventData.SendOnDateTime),
                        EmailStatus.Draft);
                }
            
        }
    }
}
