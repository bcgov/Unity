using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Notifications
{
    public interface IEmailNotificationService: IApplicationService
    {
        Task SendEmailNotification(Guid id, string body, string subject);
        string GetApprovalBody();
        string GetDeclineBody();
    }
}