using RestSharp;
using System;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.EmailNotifications
{
    public interface IEmailNotificationService: IApplicationService
    {
        Task<EmailLog?> InitializeEmailLog(string email, string body, string subject, Guid applicationId);
        Task<RestResponse> SendEmailNotification(string email, string body, string subject, Guid applicationId);
        Task SendEmailToQueue(EmailLog emailLog);
        string GetApprovalBody();
        string GetDeclineBody();
    }
}