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
        Task<EmailLog?> UpdateEmailLog(Guid emailId, EmailMessageParams email, Guid applicationId, string? status);
        Task<EmailLog?> InitializeEmailLog(EmailMessageParams email, Guid applicationId, string? status);
        Task<EmailLog?> InitializeEmailLog(EmailMessageParams email, Guid applicationId);
        Task<EmailLog?> GetEmailLogById(Guid id);
        Task<HttpResponseMessage> SendCommentNotification(EmailCommentDto input);
        Task<HttpResponseMessage> SendEmailNotification(EmailMessageParams email, string? emailBodyType = null);
        Task<HttpResponseMessage> SendEmailNotification(EmailLog emailLog);        
        Task SendEmailToQueue(EmailLog emailLog);
        Task<List<EmailHistoryDto>> GetHistoryByApplicationId(Guid applicationId);
        Task UpdateSettings(NotificationsSettingsDto settingsDto);
        Task<Guid> InitializeDraftAsync(Guid applicationId);
        Task DeleteEmail(Guid id);
        Task<int> GetEmailsChesWithNoResponseCountAsync();
    }
}
