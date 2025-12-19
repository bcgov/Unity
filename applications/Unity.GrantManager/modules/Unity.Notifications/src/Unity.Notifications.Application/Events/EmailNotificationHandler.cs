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

namespace Unity.GrantManager.Events
{
    internal class EmailNotificationHandler(
            IEmailNotificationService emailNotificationService,
            IFeatureChecker featureChecker,
            EmailAttachmentService emailAttachmentService,
            IEmailLogsRepository emailLogsRepository,
            ILogger<EmailNotificationHandler> logger) : ILocalEventHandler<EmailNotificationEvent>, ITransientDependency
    {
        private const string FAILED_PAYMENTS_SUBJECT = "CAS Payment Failure Notification";

        public async Task HandleEventAsync(EmailNotificationEvent eventData)
        {
            if (await featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                await EmailNotificationEventAsync(eventData);
            }
        }

        private async Task InitializeAndSendEmailToQueue(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? emailTemplateName, string? emailCC = null, string? emailBCC = null, List<EmailAttachmentData>? emailAttachments = null)
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
                // Email will still be sent below, just without attachments
                // even if S3 upload failed - recipients will notice missing attachment
            }

            // Queue email for sending
            await emailNotificationService.SendEmailToQueue(emailLog);
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

        private async Task EmailNotificationEventAsync(EmailNotificationEvent eventData)
        {
            if (eventData == null) return;
            
            switch (eventData.Action)
            {
                case EmailAction.SendFailedSummary:
     
                        string emailToAddress = String.Join(",", eventData.EmailAddressList);

                        await InitializeAndSendEmailToQueue(emailToAddress, eventData.Body, FAILED_PAYMENTS_SUBJECT, eventData.ApplicationId, eventData.EmailFrom,eventData.EmailTemplateName);
                    
                    break;

                case EmailAction.SendCustom:
                    await HandleSendCustomEmail(eventData);
                    break;

                case EmailAction.SaveDraft:
                    await HandleSaveDraftEmail(eventData);
                    break;

                case EmailAction.SendFsbNotification:
                    string fsbEmailToAddress = String.Join(",", eventData.EmailAddressList);
                    await InitializeAndSendEmailToQueue(
                        fsbEmailToAddress,
                        eventData.Body,
                        eventData.Subject ?? "FSB Payment Notification",
                        eventData.ApplicationId,
                        eventData.EmailFrom,
                        eventData.EmailTemplateName,
                        null, // emailCC
                        null, // emailBCC
                        eventData.EmailAttachments);
                    break;

                case EmailAction.Retry:
                    break;
            }
        }

        private async Task HandleSendCustomEmail(EmailNotificationEvent eventData)
        {

           
                string emailToAddress = String.Join(",", eventData.EmailAddressList);
                string? emailCC = eventData.Cc?.Any() == true ? String.Join(",", eventData.Cc) : null;
                string? emailBCC = eventData.Bcc?.Any() == true ? String.Join(",", eventData.Bcc) : null;
                
                if (eventData.Id == Guid.Empty)
                {
                    await InitializeAndSendEmailToQueue(emailToAddress, eventData.Body, eventData.Subject, eventData.ApplicationId, eventData.EmailFrom, eventData.EmailTemplateName, emailCC, emailBCC);
                }
                else
                {
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
                        await emailNotificationService.SendEmailToQueue(emailLog);
                    }
                    else
                    {
                        throw new UserFriendlyException("Unable to update Email Log");
                    }
                }
            
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
