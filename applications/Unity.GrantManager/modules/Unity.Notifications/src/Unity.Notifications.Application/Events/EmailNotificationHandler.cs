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

        private async Task<EmailLog> InitializeEmailAndUploadAttachments(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? emailTemplateName, string? emailCC = null, string? emailBCC = null, List<EmailAttachmentData>? emailAttachments = null)
        {
            EmailLog emailLog = await InitializeEmail(
                                                emailTo,
                                                body,
                                                subject,
                                                applicationId,
                                                emailFrom,
                                                EmailStatus.Initialized,
                                                emailTemplateName,
                                                emailCC,
                                                emailBCC);
            
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

        private async Task<EmailLog> InitializeEmail(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
        {
            EmailLog emailLog = await emailNotificationService.InitializeEmailLog(
                                                emailTo,
                                                body,
                                                subject,
                                                applicationId,
                                                emailFrom,
                                                status,
                                                emailTemplateName,
                                                emailCC,
                                                emailBCC) ?? throw new UserFriendlyException("Unable to Initialize Email Log");
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
                        emailToAddress,
                        eventData.Body,
                        FAILED_PAYMENTS_SUBJECT,
                        eventData.ApplicationId,
                        eventData.EmailFrom,
                        eventData.EmailTemplateName);
                }
                case EmailAction.SendCustom:
                    return await HandleSendCustomEmail(eventData);

                case EmailAction.SaveDraft:
                    await HandleSaveDraftEmail(eventData);
                    return null;

                case EmailAction.SendFsbNotification:
                {
                    string fsbEmailToAddress = String.Join(",", eventData.EmailAddressList);
                    return await InitializeEmailAndUploadAttachments(
                        fsbEmailToAddress,
                        eventData.Body,
                        eventData.Subject ?? "FSB Payment Notification",
                        eventData.ApplicationId,
                        eventData.EmailFrom,
                        eventData.EmailTemplateName,
                        null, // emailCC
                        null, // emailBCC
                        eventData.EmailAttachments);
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
            
            if (eventData.Id == Guid.Empty)
            {
                return await InitializeEmailAndUploadAttachments(
                    emailToAddress,
                    eventData.Body,
                    eventData.Subject,
                    eventData.ApplicationId,
                    eventData.EmailFrom,
                    eventData.EmailTemplateName,
                    emailCC,
                    emailBCC,
                    eventData.EmailAttachments);
            }

            EmailLog? emailLog = await emailNotificationService.UpdateEmailLog(
                eventData.Id,
                emailToAddress,
                eventData.Body,
                eventData.Subject,
                eventData.ApplicationId,
                eventData.EmailFrom,
                EmailStatus.Initialized,
                eventData.EmailTemplateName,
                emailCC,
                emailBCC);

            if (emailLog != null)
            {
                return emailLog;
            }

            throw new UserFriendlyException("Unable to update Email Log");
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
                        emailToAddress,
                        eventData.Body,
                        eventData.Subject,
                        eventData.ApplicationId,
                        eventData.EmailFrom,
                        EmailStatus.Draft,
                        eventData.EmailTemplateName,
                        emailCC,
                        emailBCC);
                }
                else
                {
                    await InitializeEmail(
                        emailToAddress,
                        eventData.Body,
                        eventData.Subject,
                        eventData.ApplicationId,
                        eventData.EmailFrom,
                        EmailStatus.Draft, 
                        eventData.EmailTemplateName,
                        emailCC,
                        emailBCC);
                }
            
        }
    }
}
