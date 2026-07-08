using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Integrations.Ches;
using Unity.Notifications.Integrations.RabbitMQ;
using Unity.Notifications.Settings;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Services;
using Volo.Abp.Settings;

namespace Unity.Notifications.EmailNotifications
{
    /// <summary>
    /// Domain manager for email notification operations
    /// </summary>
    public class EmailNotificationManager(
        IEmailLogsRepository emailLogsRepository,
        IChesClientService chesClientService,
        EmailQueueService emailQueueService,
        EmailAttachmentService emailAttachmentService,
        ISettingProvider settingProvider) : DomainService, IEmailNotificationManager
    {
        private static readonly TimeSpan BcPermanentDstOffset = TimeSpan.FromHours(-7);

        public async Task<EmailLog?> CreateEmailLogAsync(EmailMessageParams email, Guid applicationId)
        {
            return await CreateEmailLogAsync(email, applicationId, EmailStatus.Initialized);
        }

        [RemoteService(false)]
        public async Task<EmailLog?> CreateEmailLogAsync(EmailMessageParams email, Guid applicationId, string? status, Guid? scheduledNotificationId = null)
        {
            if (string.IsNullOrEmpty(email.EmailTo))
            {
                return null;
            }
            var emailLog = new EmailLog { Id = GuidGenerator.Create() };
            emailLog = await PopulateEmailLogAsync(emailLog, email, applicationId, status, scheduledNotificationId);
            return await emailLogsRepository.InsertAsync(emailLog, autoSave: true);
        }

        public async Task<EmailLog?> UpdateEmailLogAsync(Guid emailId, EmailMessageParams email, Guid applicationId, string? status)
        {
            if (string.IsNullOrEmpty(email.EmailTo))
            {
                return null;
            }

            var existingEmail = await emailLogsRepository.FindAsync(emailId);
            
            if (existingEmail == null)
            {
                // Email doesn't exist, create a new one instead
                var newEmailLog = new EmailLog { Id = emailId };
                newEmailLog = await PopulateEmailLogAsync(newEmailLog, email, applicationId, status);
                return await emailLogsRepository.InsertAsync(newEmailLog, autoSave: true);
            }
            
            // Email exists, update it
            existingEmail = await PopulateEmailLogAsync(existingEmail, email, applicationId, status);
            return await emailLogsRepository.UpdateAsync(existingEmail, autoSave: true);
        }

        public async Task<EmailLog?> GetEmailLogByIdAsync(Guid id)
        {
            try
            {
                return await emailLogsRepository.GetAsync(id);
            }
            catch (EntityNotFoundException ex)
            {
                Logger.LogError(ex, "Entity not found for Email Log. Tenant context may be incorrect: {ExceptionMessage}", ex.Message);
                return null;
            }
        }

        public async Task<EmailLog> CreateDraftEmailLogAsync(Guid applicationId)
        {
            var emailLog = new EmailLog
            {
                ApplicationId = applicationId,
                Status = EmailStatus.Draft
            };
            return await emailLogsRepository.InsertAsync(emailLog, autoSave: true);
        }

        public async Task DeleteEmailLogAsync(Guid id)
        {
            var emailLog = await emailLogsRepository.GetAsync(id);
            if (emailLog.Status == EmailStatus.Sent && !emailLog.SendOnDateTime.HasValue)
            {
                throw new UserFriendlyException("Sent emails cannot be deleted.");
            }

            if (emailLog.SendOnDateTime.HasValue && emailLog.ChesMsgId.HasValue)
            {
                var shouldThrow = await SyncScheduledEmailStatusAsync(emailLog);
                if (shouldThrow)
                {
                    // Update the entity in the current context and persist it
                    await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
                    throw new UserFriendlyException(
                        "This scheduled email has already been sent and cannot be deleted.");
                }
            }

            await DeleteEmailAttachmentsAsync(id);
            
            try
            {
                await emailLogsRepository.DeleteAsync(id);
            }
            catch (AbpDbConcurrencyException)
            {
                // Handle concurrency exception - entity may have been deleted by another request
                // Try to get fresh entity and delete it, or silently succeed if it's already gone
                var freshEmailLog = await emailLogsRepository.FindAsync(id);
                if (freshEmailLog != null)
                {
                    await emailLogsRepository.DeleteAsync(freshEmailLog, autoSave: true);
                }
                // If entity doesn't exist, that's fine - it was already deleted
            }
        }

        public async Task CancelEmailLogAsync(Guid id)
        {
            var emailLog = await emailLogsRepository.GetAsync(id);
            if (emailLog.Status == EmailStatus.Sent)
            {
                throw new UserFriendlyException("Sent emails cannot be cancelled.");
            }

            emailLog.Status = EmailStatus.Cancelled;
            await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
        }

        /// <summary>
        /// Send Email Notification
        /// </summary>
        /// <param name="emailTo">The email address to send to</param>
        /// <param name="body">The body of the email</param>
        /// <param name="subject">Subject Message</param>
        /// <param name="emailFrom">From Email Address</param>
        /// <param name="emailBodyType">Type of body email: html or text</param>
        /// <param name="emailTemplateName">Template name for the email</param>
        /// <param name="emailCC">CC email addresses</param>
        /// <param name="emailBCC">BCC email addresses</param>
        /// <returns>HttpResponseMessage indicating the result of the operation</returns>
        public async Task<HttpResponseMessage> SendEmailAsync(EmailMessageParams email, string? emailBodyType = null)
        {
            if (string.IsNullOrEmpty(email.EmailTo))
            {
                Logger.LogError("EmailNotificationManager->SendEmailAsync: The 'emailTo' parameter is null or empty.");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("'emailTo' cannot be null or empty.")
                };
            }

            return await SendEmailInternalAsync(
                () => GetEmailObjectAsync(email, emailBodyType, excludeTemplate: true),
                "EmailNotificationManager->SendEmailAsync: Exception occurred while sending email.");
        }

        /// <summary>
        /// Send Email Notification from EmailLog (with S3 attachments support)
        /// </summary>
        /// <param name="emailLog">The email log containing email details</param>
        /// <returns>HttpResponseMessage indicating the result of the operation</returns>
        [RemoteService(false)]
        public async Task<HttpResponseMessage> SendEmailAsync(EmailLog emailLog)
        {
            if (string.IsNullOrEmpty(emailLog.ToAddress))
            {
                Logger.LogError("EmailNotificationManager->SendEmailAsync: The 'emailLog.ToAddress' parameter is null or empty.");
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("'emailLog.ToAddress' cannot be null or empty.")
                };
            }

            return await SendEmailInternalAsync(
                () => BuildEmailObjectWithAttachmentsAsync(emailLog),
                $"EmailNotificationManager->SendEmailAsync: Exception occurred while sending email for EmailLog {emailLog.Id}.");
        }

        /// <summary>
        /// Send Email To Queue
        /// </summary>
        /// <param name="emailLog">The email log to send to queue</param>
        public async Task QueueEmailAsync(EmailLog emailLog)
        {
            EmailNotificationEvent emailNotificationEvent = new()
            {
                Id = emailLog.Id,
                TenantId = emailLog.TenantId,
                RetryAttempts = emailLog.RetryAttempts
            };
            await emailQueueService.SendToEmailEventQueueAsync(emailNotificationEvent);
        }

        public async Task<int> GetPendingEmailsCountAsync()
        {
            var dbNow = DateTime.UtcNow;

            // Create the expression to filter the email logs
            Expression<Func<EmailLog, bool>> filter = x =>
                (x.Status == EmailStatus.Sent && x.ChesResponse == null) ||
                (x.Status == EmailStatus.Initialized && x.CreationTime.AddMinutes(10) < dbNow);

            // Fetch all email logs and apply the filter using LINQ
            var allEmailLogs = await emailLogsRepository.GetListAsync();
            var emailLogs = allEmailLogs.Where(filter.Compile()).ToList();

            return emailLogs.Count;
        }

        public async Task<dynamic> BuildEmailObjectWithAttachmentsAsync(EmailLog emailLog)
        {
            // Get base email object (without attachments)
            var emailObject = await GetEmailObjectAsync(
                new EmailMessageParams(emailLog.ToAddress, emailLog.Body, emailLog.Subject,
                    emailLog.FromAddress, emailLog.TemplateName, emailLog.CC, emailLog.BCC, emailLog.SendOnDateTime),
                emailLog.BodyType,
                excludeTemplate: true);

            // Retrieve and add attachments from S3
            var attachments = await emailAttachmentService.GetAttachmentsAsync(emailLog.Id);
            if (attachments.Count != 0)
            {
                var attachmentList = new List<object>();
                foreach (var attachment in attachments)
                {
                    byte[]? content = await emailAttachmentService.DownloadFromS3Async(attachment.S3ObjectKey);
                    if (content != null)
                    {
                        attachmentList.Add(CreateAttachmentObject(attachment, content));
                    }
                }

                var emailObjectDictionary = (IDictionary<string, object?>)emailObject;
                emailObjectDictionary["attachments"] = attachmentList.ToArray();
            }

            return emailObject;
        }

        protected virtual async Task<dynamic> GetEmailObjectAsync(
            EmailMessageParams email,
            string? emailBodyType = null,
            bool excludeTemplate = false)
        {
            var toList = email.EmailTo.ParseEmailList() ?? [];
            var ccList = email.EmailCC.ParseEmailList();
            var bccList = email.EmailBCC.ParseEmailList();

            var defaultFromAddress = await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);

            dynamic emailObject = new ExpandoObject();
            var emailObjectDictionary = (IDictionary<string, object?>)emailObject;

            emailObjectDictionary["body"] = email.Body;
            emailObjectDictionary["bodyType"] = emailBodyType ?? "text";
            emailObjectDictionary["encoding"] = "utf-8";
            emailObjectDictionary["from"] = email.EmailFrom ?? defaultFromAddress ?? "NoReply@gov.bc.ca";
            emailObjectDictionary["priority"] = "normal";
            emailObjectDictionary["subject"] = email.Subject;
            emailObjectDictionary["tag"] = "tag";
            emailObjectDictionary["to"] = toList;

            // Only include cc/bcc when provided CHES API expects arrays, not null.
            if (ccList != null)
            {
                emailObjectDictionary["cc"] = ccList;
            }
            if (bccList != null)
            {
                emailObjectDictionary["bcc"] = bccList;
            }

            // delayTS: desired UTC send time as Unix milliseconds; 0 = send immediately.
            if (email.SendOnDateTime.HasValue)
            {
                var normalizedUtcSendOn = NormalizeToUtc(email.SendOnDateTime.Value);
                emailObjectDictionary["delayTS"] = new DateTimeOffset(normalizedUtcSendOn).ToUnixTimeMilliseconds();
            }

            // templateName is not part of the CHES MessageObject schema
            // store it on the EmailLog but don't send it to the API.
            if (!excludeTemplate)
            {
                emailObjectDictionary["templateName"] = email.EmailTemplateName;
            }

            return emailObject;
        }

        private static DateTime NormalizeToUtc(DateTime sendOnDateTime)
        {
            return sendOnDateTime.Kind switch
            {
                DateTimeKind.Utc => sendOnDateTime,
                DateTimeKind.Local => sendOnDateTime.ToUniversalTime(),
                _ => new DateTimeOffset(sendOnDateTime, BcPermanentDstOffset).UtcDateTime
            };
        }

        protected virtual EmailLog UpdateMappedEmailLog(EmailLog emailLog, dynamic emailDynamicObject)
        {
            var dict = (IDictionary<string, object?>)emailDynamicObject;
            emailLog.Body = emailDynamicObject.body;
            emailLog.Subject = emailDynamicObject.subject;
            emailLog.BodyType = emailDynamicObject.bodyType;
            emailLog.FromAddress = emailDynamicObject.from;
            emailLog.ToAddress = string.Join(",", emailDynamicObject.to);
            emailLog.CC = dict.TryGetValue("cc", out var cc) && cc is IEnumerable<string> ccList
                ? string.Join(",", ccList)
                : string.Empty;
            emailLog.BCC = dict.TryGetValue("bcc", out var bcc) && bcc is IEnumerable<string> bccList
                ? string.Join(",", bccList)
                : string.Empty;
            emailLog.TemplateName = dict.TryGetValue("templateName", out var templateName) && templateName is string templateNameStr
                ? templateNameStr
                : string.Empty;
            return emailLog;
        }

        public async Task<List<EmailLog>> GetEmailLogsByApplicationIdAsync(Guid applicationId)
        {
            return await emailLogsRepository.GetByApplicationIdAsync(applicationId);
        }

        /// <summary>
        /// Populates common email log fields used in create and update operations
        /// </summary>
        private async Task<EmailLog> PopulateEmailLogAsync(
            EmailLog emailLog,
            EmailMessageParams email,
            Guid applicationId,
            string? status,
            Guid? scheduledNotificationId = null)
        {
            var emailObject = await GetEmailObjectAsync(email, "html");
            emailLog = UpdateMappedEmailLog(emailLog, emailObject);
            emailLog.ApplicationId = applicationId;
            if (scheduledNotificationId.HasValue)
            {
                emailLog.ScheduledNotificationId = scheduledNotificationId.Value;
            }
            var normalizedSendOnDateTime = email.SendOnDateTime.HasValue
                ? NormalizeToUtc(email.SendOnDateTime.Value)
                : (DateTime?)null;

            emailLog.SendOnDateTime = normalizedSendOnDateTime;
            emailLog.Status = DetermineSendStatus(normalizedSendOnDateTime, status);
            emailLog.EmailType = DetermineEmailType(normalizedSendOnDateTime);
            return emailLog;
        }

        /// <summary>
        /// Sends an email via CHES with unified exception handling
        /// </summary>
        private async Task<HttpResponseMessage> SendEmailInternalAsync(
            Func<Task<dynamic>> buildEmailObject,
            string errorMessage)
        {
            try
            {
                var emailObject = await buildEmailObject();
                return await chesClientService.SendAsync(emailObject);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, errorMessage);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"An exception occurred while sending the email: {ex.Message}")
                };
            }
        }

        /// <summary>
        /// Synchronizes the status of a scheduled email with CHES
        /// </summary>
        /// <returns>True if the email has already been sent (completed/accepted), false otherwise</returns>
        private async Task<bool> SyncScheduledEmailStatusAsync(EmailLog emailLog)
        {
            try
            {
                var statusResponse = await chesClientService.GetStatusAsync(emailLog.ChesMsgId!.Value);
                if (statusResponse == null || !statusResponse.IsSuccessStatusCode)
                {
                    Logger.LogWarning(
                        "CHES status check returned unsuccessful status code {StatusCode} for MessageId {MessageId}",
                        statusResponse?.StatusCode,
                        emailLog.ChesMsgId);
                    return false;
                }

                var responseContent = await statusResponse.Content.ReadAsStringAsync();
                Logger.LogInformation(
                    "CHES status check for MessageId {MessageId}: {StatusResponse}",
                    emailLog.ChesMsgId,
                    responseContent);

                var status = ExtractChesStatus(responseContent);
                if (string.IsNullOrWhiteSpace(status))
                {
                    return false;
                }

                if (status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("accepted", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation("Email {EmailLogId} CHES status is {ChesStatus}, marking as Sent", emailLog.Id, status);
                    
                    // Just update the entity properties - caller will persist it
                    emailLog.Status = EmailStatus.Sent;
                    emailLog.ChesStatus = status.Capitalize();
                    
                    return true;
                }

                await UpdateChesStatusAsync(emailLog, status);

                if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    await CancelPendingEmailAsync(emailLog);
                }

                return false;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error checking CHES status for scheduled email {EmailLogId}",
                    emailLog.Id);
                throw new UserFriendlyException(
                    "Unable to verify the status of the scheduled email. Please try again.");
            }
        }

        /// <summary>
        /// Extracts the status from a CHES status response
        /// </summary>
        private static string? ExtractChesStatus(string responseContent)
        {
            dynamic? statusData = JsonConvert.DeserializeObject(responseContent);
            if (statusData != null)
            {
                // Handle array response - get first element if it's an array
                if (statusData is Newtonsoft.Json.Linq.JArray jArray && jArray.Count > 0)
                {
                    statusData = jArray[0];
                }
                return statusData.status?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Updates the CHES status in the email log
        /// </summary>
        private async Task UpdateChesStatusAsync(EmailLog emailLog, string status)
        {
            if (string.IsNullOrEmpty(status) || status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            emailLog.ChesStatus = status.Capitalize();
            await emailLogsRepository.UpdateAsync(emailLog);
            Logger.LogInformation(
                "Updated email {EmailLogId} status to {Status}",
                emailLog.Id,
                emailLog.ChesStatus);
        }

        /// <summary>
        /// Cancels a pending email via CHES
        /// </summary>
        private async Task CancelPendingEmailAsync(EmailLog emailLog)
        {
            try
            {
                var cancelResponse = await chesClientService.CancelEmailAsync(emailLog.ChesMsgId!.Value);
                if (cancelResponse == null || !cancelResponse.IsSuccessStatusCode)
                {
                    Logger.LogWarning(
                        "Failed to cancel pending email {EmailLogId} with MessageId {MessageId}. Status: {StatusCode}",
                        emailLog.Id,
                        emailLog.ChesMsgId,
                        cancelResponse?.StatusCode);
                    throw new UserFriendlyException(
                        "Unable to cancel the pending scheduled email. Please try again.");
                }
                Logger.LogInformation(
                    "Successfully cancelled pending email {EmailLogId} with MessageId {MessageId}",
                    emailLog.Id,
                    emailLog.ChesMsgId);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Error cancelling pending email {EmailLogId} with MessageId {MessageId}",
                    emailLog.Id,
                    emailLog.ChesMsgId);
                throw new UserFriendlyException(
                    "Unable to cancel the pending scheduled email. Please try again.");
            }
        }

        /// <summary>
        /// Deletes all S3 attachments for an email log
        /// </summary>
        private async Task DeleteEmailAttachmentsAsync(Guid emailLogId)
        {
            var attachments = await emailAttachmentService.GetAttachmentsAsync(emailLogId);
            foreach (var attachment in attachments)
            {
                try
                {
                    await emailAttachmentService.DeleteFromS3Async(attachment.S3ObjectKey);
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        ex,
                        "Failed to delete S3 attachment with key: {S3ObjectKey}",
                        attachment.S3ObjectKey);
                }
            }
        }

        /// <summary>
        /// Creates an attachment object for CHES API submission
        /// </summary>
        private static object CreateAttachmentObject(EmailLogAttachment attachment, byte[] content)
        {
            return new
            {
                content = Convert.ToBase64String(content),
                contentType = attachment.ContentType,
                encoding = "base64",
                filename = attachment.FileName
            };
        }

        /// <summary>
        /// Determines the appropriate email status based on scheduled send time
        /// </summary>
        /// <param name="sendOnDateTime">The scheduled send time, if any</param>
        /// <param name="defaultStatus">The default status to use if not scheduled</param>
        /// <returns>Either Scheduled (if future date) or the default status</returns>
        private string DetermineSendStatus(DateTime? sendOnDateTime, string? defaultStatus)
        {
            if (sendOnDateTime.HasValue && sendOnDateTime.Value > DateTime.UtcNow)
            {
                return EmailStatus.Scheduled;
            }
            return defaultStatus ?? EmailStatus.Initialized;
        }

        /// <summary>
        /// Determines the appropriate email type based on scheduled send time
        /// </summary>
        /// <param name="sendOnDateTime">The scheduled send time, if any</param>
        /// <returns>EmailType.Delayed if future date is set, otherwise EmailType.Manual</returns>
        private EmailType DetermineEmailType(DateTime? sendOnDateTime)
        {
            if (sendOnDateTime.HasValue && sendOnDateTime.Value > DateTime.UtcNow)
            {
                return EmailType.Delayed;
            }
            return EmailType.Manual;
        }
    }
}
