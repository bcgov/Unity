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
        Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom, string? status);
        Task<EmailLog?> InitializeEmailLog(string emailTo, string body, string subject, Guid applicationId, string? emailFrom);
        Task<EmailLog?> GetEmailLogById(Guid id);
        Task<HttpResponseMessage> SendEmailNotification(string emailTo, string body, string subject, string? emailFrom);
        Task SendEmailToQueue(EmailLog emailLog);
        string GetApprovalBody();
        string GetDeclineBody();
        Task<List<EmailHistoryDto>> GetHistoryByApplicationId(Guid applicationId);
        Task UpdateSettings(NotificationsSettingsDto settingsDto);
    }
}