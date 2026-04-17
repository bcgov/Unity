using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Notifications.Emails;

namespace Unity.Notifications.EmailNotifications
{
    /// <summary>
    /// Domain manager for email notification operations
    /// </summary>
    public interface IEmailNotificationManager
    {
        /// <summary>
        /// Creates and initializes a new email log
        /// </summary>
        Task<EmailLog?> CreateEmailLogAsync(EmailMessageParams email, Guid applicationId);

        /// <summary>
        /// Creates and initializes a new email log with status
        /// </summary>
        Task<EmailLog?> CreateEmailLogAsync(EmailMessageParams email, Guid applicationId, string? status);

        /// <summary>
        /// Updates an existing email log
        /// </summary>
        Task<EmailLog?> UpdateEmailLogAsync(Guid emailId, EmailMessageParams email, Guid applicationId, string? status);

        /// <summary>
        /// Retrieves an email log by ID
        /// </summary>
        Task<EmailLog?> GetEmailLogByIdAsync(Guid id);

        /// <summary>
        /// Creates an empty draft email log for composing
        /// </summary>
        Task<EmailLog> CreateDraftEmailLogAsync(Guid applicationId);

        /// <summary>
        /// Deletes an email log and its S3 attachments
        /// </summary>
        Task DeleteEmailLogAsync(Guid id);

        /// <summary>
        /// Sends an email notification using CHES
        /// </summary>
        Task<HttpResponseMessage> SendEmailAsync(EmailMessageParams email, string? emailBodyType = null);

        /// <summary>
        /// Sends an email notification from an EmailLog (with S3 attachments support)
        /// </summary>
        Task<HttpResponseMessage> SendEmailAsync(EmailLog emailLog);

        /// <summary>
        /// Queues an email for batch processing
        /// </summary>
        Task QueueEmailAsync(EmailLog emailLog);

        /// <summary>
        /// Gets the count of pending emails (sent without response or initialized > 10 minutes)
        /// </summary>
        Task<int> GetPendingEmailsCountAsync();

        /// <summary>
        /// Builds an email object with attachments from S3
        /// </summary>
        Task<dynamic> BuildEmailObjectWithAttachmentsAsync(EmailLog emailLog);

        /// <summary>
        /// Gets all email logs for a specific application
        /// </summary>
        Task<List<EmailLog>> GetEmailLogsByApplicationIdAsync(Guid applicationId);
    }
}
