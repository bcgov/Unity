using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.EmailNotifications
{
    public interface IEmailNotificationService: IApplicationService
    {
        Task SendEmailNotification(string email, string body, string subject);
        string GetApprovalBody();
        string GetDeclineBody();
    }
}