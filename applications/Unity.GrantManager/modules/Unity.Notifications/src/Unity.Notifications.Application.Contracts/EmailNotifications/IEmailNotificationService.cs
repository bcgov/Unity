using RestSharp;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.EmailNotifications
{
    public interface IEmailNotificationService: IApplicationService
    {
        Task<RestResponse> SendEmailNotification(string email, string body, string subject, Guid applicationId);
        Task SendEmaiToQueue(string email, string body, string subject, Guid applicationId);
        string GetApprovalBody();
        string GetDeclineBody();
    }
}