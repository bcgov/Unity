using Microsoft.Extensions.Logging;
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
        public async Task<EmailLog?> CreateEmailLogAsync(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
        {
            return await CreateEmailLogAsync(emailTo, body, subject, applicationId, emailFrom, EmailStatus.Initialized, emailTemplateName, emailCC, emailBCC);
        }

        [RemoteService(false)]
        public async Task<EmailLog?> CreateEmailLogAsync(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
        {
            if (string.IsNullOrEmpty(emailTo))
            {
                return null;
            }
            var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom, "html", emailTemplateName, emailCC, emailBCC);
            EmailLog emailLog = new();
            emailLog = UpdateMappedEmailLog(emailLog, emailObject);
            emailLog.ApplicationId = applicationId;
            emailLog.Status = status ?? EmailStatus.Initialized;

            // When being called here the current tenant is in context - verified by looking at the tenant id
            EmailLog loggedEmail = await emailLogsRepository.InsertAsync(emailLog, autoSave: true);
            return loggedEmail;
        }

        public async Task<EmailLog?> UpdateEmailLogAsync(Guid emailId, string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
        {
            if (string.IsNullOrEmpty(emailTo))
            {
                return null;
            }

            var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom, "html", emailTemplateName, emailCC, emailBCC);
            EmailLog emailLog = await emailLogsRepository.GetAsync(emailId);
            emailLog = UpdateMappedEmailLog(emailLog, emailObject);
            emailLog.ApplicationId = applicationId;
            emailLog.Id = emailId;
            emailLog.Status = status ?? EmailStatus.Initialized;

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

        public async Task DeleteEmailLogAsync(Guid id)
        {
            await emailLogsRepository.DeleteAsync(id);
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
        public async Task<HttpResponseMessage> SendEmailAsync(string emailTo, string body, string subject, string? emailFrom, string? emailBodyType, string? emailTemplateName, string? emailCC = null, string? emailBCC = null)
        {
            try
            {
                if (string.IsNullOrEmpty(emailTo))
                {
                    Logger.LogError("EmailNotificationManager->SendEmailAsync: The 'emailTo' parameter is null or empty.");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("'emailTo' cannot be null or empty.")
                    };
                }

                // Send the email using the CHES client service
                var emailObject = await GetEmailObjectAsync(emailTo, body, subject, emailFrom, emailBodyType, emailTemplateName, emailCC, emailBCC);
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
                if (emailLog == null)
                {
                    Logger.LogError("EmailNotificationManager->SendEmailAsync: The 'emailLog' parameter is null.");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("'emailLog' cannot be null.")
                    };
                }

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
                Logger.LogError(ex, "EmailNotificationManager->SendEmailAsync: Exception occurred while sending email for EmailLog {EmailId}.", emailLog?.Id);
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

            // Ensure we're returning 0 if no logs are found
            return emailLogs?.Count ?? 0;
        }

        public async Task<dynamic> BuildEmailObjectWithAttachmentsAsync(EmailLog emailLog)
        {
            // Get base email object (without attachments)
            var emailObject = await GetEmailObjectAsync(
                emailLog.ToAddress,
                emailLog.Body,
                emailLog.Subject,
                emailLog.FromAddress,
                emailLog.BodyType,
                emailLog.TemplateName,
                emailLog.CC,
                emailLog.BCC);

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

            return emailObject;
        }

        protected virtual async Task<dynamic> GetEmailObjectAsync(
                                                    string emailTo,
                                                    string body,
                                                    string subject,
                                                    string? emailFrom,
                                                    string? emailBodyType,
                                                    string? emailTemplateName,
                                                    string? emailCC = null,
                                                    string? emailBCC = null)
        {
            var toList = emailTo.ParseEmailList() ?? [];
            var ccList = emailCC.ParseEmailList();
            var bccList = emailBCC.ParseEmailList();

            var defaultFromAddress = await settingProvider.GetOrNullAsync(NotificationsSettings.Mailing.DefaultFromAddress);

            dynamic emailObject = new ExpandoObject();
            var emailObjectDictionary = (IDictionary<string, object?>)emailObject;

            emailObjectDictionary["body"] = body;
            emailObjectDictionary["bodyType"] = emailBodyType ?? "text";
            emailObjectDictionary["cc"] = ccList;
            emailObjectDictionary["bcc"] = bccList;
            emailObjectDictionary["encoding"] = "utf-8";
            emailObjectDictionary["from"] = emailFrom ?? defaultFromAddress ?? "NoReply@gov.bc.ca";
            emailObjectDictionary["priority"] = "normal";
            emailObjectDictionary["subject"] = subject;
            emailObjectDictionary["tag"] = "tag";
            emailObjectDictionary["to"] = toList;
            emailObjectDictionary["templateName"] = emailTemplateName;

            return emailObject;
        }

        protected virtual EmailLog UpdateMappedEmailLog(EmailLog emailLog, dynamic emailDynamicObject)
        {
            emailLog.Body = emailDynamicObject.body;
            emailLog.Subject = emailDynamicObject.subject;
            emailLog.BodyType = emailDynamicObject.bodyType;
            emailLog.FromAddress = emailDynamicObject.from;
            emailLog.ToAddress = string.Join(",", emailDynamicObject.to);
            emailLog.CC = emailDynamicObject.cc != null ? string.Join(",", (IEnumerable<string>)emailDynamicObject.cc) : string.Empty;
            emailLog.BCC = emailDynamicObject.bcc != null ? string.Join(",", (IEnumerable<string>)emailDynamicObject.bcc) : string.Empty;
            emailLog.TemplateName = emailDynamicObject.templateName;
            return emailLog;
        }

        public async Task<List<EmailLog>> GetEmailLogsByApplicationIdAsync(Guid applicationId)
        {
            return await emailLogsRepository.GetByApplicationIdAsync(applicationId);
        }
    }
}
