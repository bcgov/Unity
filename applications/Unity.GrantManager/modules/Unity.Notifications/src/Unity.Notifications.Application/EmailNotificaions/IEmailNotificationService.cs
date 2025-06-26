using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Unity.Notifications.Settings;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.EmailNotifications
{
    public interface IEmailNotificationService : IApplicationService
    {
        Task<EmailLog?> UpdateEmailLog(Guid emailId, string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null);
        Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status, string? emailTemplateName, string? emailCC = null, string? emailBCC = null);
        Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? emailTemplateName, string? emailCC = null, string? emailBCC = null);
        Task<EmailLog?> GetEmailLogById(Guid id);
        Task<HttpResponseMessage> SendCommentNotification(EmailCommentDto input);
        Task<HttpResponseMessage> SendEmailNotification(string emailTo, string body, string subject, string? emailFrom, string? emailBodyType, string? emailTemplateName, string? emailCC = null, string? emailBCC = null);
        Task SendEmailToQueue(EmailLog emailLog);
        string GetApprovalBody();
        string GetDeclineBody();
        Task<List<EmailHistoryDto>> GetHistoryByApplicationId(Guid applicationId);
        Task UpdateSettings(NotificationsSettingsDto settingsDto);
        Task DeleteEmail(Guid id);
    }
}