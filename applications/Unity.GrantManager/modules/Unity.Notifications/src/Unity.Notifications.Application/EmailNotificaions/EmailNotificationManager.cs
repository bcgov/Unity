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
            var emailObject = await GetEmailObjectAsync(email, "html");
            EmailLog emailLog = new() { Id = GuidGenerator.Create() };
            emailLog = UpdateMappedEmailLog(emailLog, emailObject);
            emailLog.ApplicationId = applicationId;
            emailLog.ScheduledNotificationId = scheduledNotificationId;
            emailLog.SendOnDateTime = email.SendOnDateTime;
            
            // If SendOnDateTime is in the future, set status to Scheduled instead of the provided status
            if (email.SendOnDateTime.HasValue && email.SendOnDateTime.Value > DateTime.UtcNow)
            {
                emailLog.Status = EmailStatus.Scheduled;
            }
            else
            {
                emailLog.Status = status ?? EmailStatus.Initialized;
            }

            // When being called here the current tenant is in context - verified by looking at the tenant id
            EmailLog loggedEmail = await emailLogsRepository.InsertAsync(emailLog, autoSave: true);
            return loggedEmail;
        }

        public async Task<EmailLog?> UpdateEmailLogAsync(Guid emailId, EmailMessageParams email, Guid applicationId, string? status)
        {
            if (string.IsNullOrEmpty(email.EmailTo))
            {
                return null;
            }

            var emailObject = await GetEmailObjectAsync(email, "html");
            EmailLog emailLog = await emailLogsRepository.GetAsync(emailId);
            emailLog = UpdateMappedEmailLog(emailLog, emailObject);
            emailLog.ApplicationId = applicationId;
            emailLog.Id = emailId;
            emailLog.SendOnDateTime = email.SendOnDateTime;
            
            // If SendOnDateTime is in the future, set status to Scheduled instead of the provided status
            if (email.SendOnDateTime.HasValue && email.SendOnDateTime.Value > DateTime.UtcNow)
            {
                emailLog.Status = EmailStatus.Scheduled;
            }
            else
            {
                emailLog.Status = status ?? EmailStatus.Initialized;
            }

            // When being called here the current tenant is in context - verified by looking at the tenant id
            EmailLog loggedEmail = await emailLogsRepository.UpdateAsync(emailLog, autoSave: true);
            return loggedEmail;
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

            // If email has a scheduled send date and a CHES message ID, check the status with CHES
            if (emailLog.SendOnDateTime.HasValue && emailLog.ChesMsgId.HasValue)
            {
                try
                {
                    var statusResponse = await chesClientService.GetStatusAsync(emailLog.ChesMsgId.Value);
                    if (statusResponse != null && statusResponse.IsSuccessStatusCode)
                    {
                        var responseContent = await statusResponse.Content.ReadAsStringAsync();
                        Logger.LogInformation("CHES status check for MessageId {MessageId}: {StatusResponse}", emailLog.ChesMsgId, responseContent);
                        
                        // Parse the CHES status response to check message status
                        try
                        {
                            dynamic? statusData = JsonConvert.DeserializeObject(responseContent);
                            string? status = null;
                            
                            if (statusData != null)
                            {
                                // Handle array response - get first element if it's an array
                                if (statusData is Newtonsoft.Json.Linq.JArray jArray && jArray.Count > 0)
                                {
                                    statusData = jArray[0];
                                }
                                
                                status = statusData.status?.ToString();
                            }

                            if (!string.IsNullOrEmpty(status) && !status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                            {
                                // Update email status with capitalized first character if status is not blank or pending
                                var capitalizedStatus = char.ToUpper(status[0]) + status.Substring(1).ToLower();
                                emailLog.ChesStatus = capitalizedStatus;
                                await emailLogsRepository.UpdateAsync(emailLog);
                                Logger.LogInformation("Updated email {EmailLogId} status to {Status}", id, capitalizedStatus);
                            }                            
                            
                            // Check if the message has already been completed or is accepted
                            if (!string.IsNullOrEmpty(status) && 
                                (status.Equals("completed", StringComparison.OrdinalIgnoreCase) || 
                                 status.Equals("accepted", StringComparison.OrdinalIgnoreCase)))
                            {
                                emailLog.ChesStatus = EmailStatus.Sent;
                                await emailLogsRepository.UpdateAsync(emailLog);                            
                                throw new UserFriendlyException("This scheduled email has already been sent and cannot be deleted.");
                            }

                            // If status is pending, attempt to cancel it via CHES
                            if (!string.IsNullOrEmpty(status) && status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    var cancelResponse = await chesClientService.CancelEmailAsync(emailLog.ChesMsgId.Value);
                                    if (cancelResponse == null || !cancelResponse.IsSuccessStatusCode)
                                    {
                                        Logger.LogWarning("Failed to cancel pending email {EmailLogId} with MessageId {MessageId}. Status: {StatusCode}", id, emailLog.ChesMsgId, cancelResponse?.StatusCode);
                                        throw new UserFriendlyException("Unable to cancel the pending scheduled email. Please try again.");
                                    }
                                    Logger.LogInformation("Successfully cancelled pending email {EmailLogId} with MessageId {MessageId}", id, emailLog.ChesMsgId);
                                }
                                catch (Exception cancelEx)
                                {
                                    Logger.LogError(cancelEx, "Error cancelling pending email {EmailLogId} with MessageId {MessageId}", id, emailLog.ChesMsgId);
                                    throw new UserFriendlyException("Unable to cancel the pending scheduled email. Please try again.");
                                }
                            }

                        }
                        catch (UserFriendlyException)
                        {
                            throw;
                        }
                        catch (Exception parseEx)
                        {
                            Logger.LogError(parseEx, "Failed to parse or process CHES status response for MessageId {MessageId}. Response content: {ResponseContent}", emailLog.ChesMsgId, responseContent);
                            throw new UserFriendlyException("Unable to verify the status of the scheduled email. Please try again.");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("CHES status check returned unsuccessful status code {StatusCode} for MessageId {MessageId}", statusResponse?.StatusCode, emailLog.ChesMsgId);
                    }
                }
                catch (UserFriendlyException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error checking CHES status for scheduled email {EmailLogId}", id);
                    throw new UserFriendlyException("Unable to verify the status of the scheduled email. Please try again.");
                }
            }

            var attachments = await emailAttachmentService.GetAttachmentsAsync(id);
            foreach (var s3Key in attachments.Select(attachment => attachment.S3ObjectKey))
            {
                try
                {
                    await emailAttachmentService.DeleteFromS3Async(s3Key);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to delete S3 attachment for EmailLog {EmailLogId}", id);
                }
            }
            await emailLogsRepository.DeleteAsync(id);
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
            try
            {
                if (string.IsNullOrEmpty(email.EmailTo))
                {
                    Logger.LogError("EmailNotificationManager->SendEmailAsync: The 'emailTo' parameter is null or empty.");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("'emailTo' cannot be null or empty.")
                    };
                }

                // Send the email using the CHES client service
                var emailObject = await GetEmailObjectAsync(email, emailBodyType, excludeTemplate: true);

                var response = await chesClientService.SendAsync(emailObject);

                // Assuming SendAsync returns a HttpResponseMessage or equivalent:
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "EmailNotificationManager->SendEmailAsync: Exception occurred while sending email.");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"An exception occurred while sending the email: {ex.Message}")
                };
            }
        }

        /// <summary>
        /// Send Email Notification from EmailLog (with S3 attachments support)
        /// </summary>
        /// <param name="emailLog">The email log containing email details</param>
        /// <returns>HttpResponseMessage indicating the result of the operation</returns>
        [RemoteService(false)]
        public async Task<HttpResponseMessage> SendEmailAsync(EmailLog emailLog)
        {
            try
            {
                if (string.IsNullOrEmpty(emailLog.ToAddress))
                {
                    Logger.LogError("EmailNotificationManager->SendEmailAsync: The 'emailLog.ToAddress' parameter is null or empty.");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("'emailLog.ToAddress' cannot be null or empty.")
                    };
                }

                // Build email object with attachments from S3
                var emailObject = await BuildEmailObjectWithAttachmentsAsync(emailLog);

                // Send via CHES
                var response = await chesClientService.SendAsync(emailObject);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "EmailNotificationManager->SendEmailAsync: Exception occurred while sending email for EmailLog {EmailId}.", emailLog.Id);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"An exception occurred while sending the email: {ex.Message}")
                };
            }
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
                    emailLog.FromAddress, emailLog.TemplateName, emailLog.CC, emailLog.BCC),
                emailLog.BodyType,
                excludeTemplate: true);

            // Retrieve attachments from S3
            var attachments = await emailAttachmentService.GetAttachmentsAsync(emailLog.Id);

            if (attachments.Count != 0)
            {
                var attachmentList = new List<object>();

                foreach (var attachment in attachments)
                {
                    byte[]? content = await emailAttachmentService.DownloadFromS3Async(attachment.S3ObjectKey);
                    if (content != null)
                    {
                        attachmentList.Add(new
                        {
                            content = Convert.ToBase64String(content),  // Convert to Base64 for CHES
                            contentType = attachment.ContentType,
                            encoding = "base64",
                            filename = attachment.FileName
                        });
                    }
                }

                var emailObjectDictionary = (IDictionary<string, object?>)emailObject;
                emailObjectDictionary["attachments"] = attachmentList.ToArray();
            }

            if (emailLog.SendOnDateTime.HasValue)
            {
                var emailObjectDictionary = (IDictionary<string, object?>)emailObject;
                emailObjectDictionary["delayTS"] = new DateTimeOffset(emailLog.SendOnDateTime.Value, TimeSpan.Zero).ToUnixTimeMilliseconds();
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
                emailObjectDictionary["delayTS"] = new DateTimeOffset(email.SendOnDateTime.Value, TimeSpan.Zero).ToUnixTimeMilliseconds();
            }

            // templateName is not part of the CHES MessageObject schema
            // store it on the EmailLog but don't send it to the API.
            if (!excludeTemplate)
            {
                emailObjectDictionary["templateName"] = email.EmailTemplateName;
            }

            return emailObject;
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
    }
}
