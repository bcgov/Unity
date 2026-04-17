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
                    string emailToAddress = String.Join(",", eventData.EmailAddressList);

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
                        await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                    }

                    return emailLog;
                }
                case EmailAction.Retry:
                default:
                    return null;
            }
        }

        private async Task<EmailLog?> HandleSendCustomEmail(EmailNotificationEvent eventData)
        {
            string emailToAddress = String.Join(",", eventData.EmailAddressList);
            string? emailCC = eventData.Cc?.Any() == true ? String.Join(",", eventData.Cc) : null;
            string? emailBCC = eventData.Bcc?.Any() == true ? String.Join(",", eventData.Bcc) : null;

            EmailLog? emailLog;
            if (eventData.Id == Guid.Empty)
            {
                emailLog = await InitializeEmailAndUploadAttachments(
                    new EmailInitParams(emailToAddress, eventData.Body, eventData.Subject,
                        eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName,
                        emailCC, emailBCC, eventData.SendOnDateTime),
                    eventData.EmailAttachments);
            }
            else
            {
                emailLog = await emailNotificationService.UpdateEmailLog(
                    eventData.Id,
                    new EmailMessageParams(emailToAddress, eventData.Body, eventData.Subject,
                        eventData.EmailFrom, eventData.EmailTemplateName, emailCC, emailBCC, eventData.SendOnDateTime),
                    eventData.ApplicationId,
                    EmailStatus.Initialized);

                if (emailLog == null)
                {
                    throw new UserFriendlyException("Unable to update Email Log");
                }
            }

            return emailLog;
        }

        private async Task HandleSaveDraftEmail(EmailNotificationEvent eventData)
        {
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
