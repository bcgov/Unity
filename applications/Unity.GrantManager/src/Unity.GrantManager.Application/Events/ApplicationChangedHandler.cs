using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Notifications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace Unity.GrantManager.Events
{
    internal class ApplicationChangedHandler : ILocalEventHandler<ApplicationChangedEvent>, ITransientDependency
    {

        private readonly IEmailNotificationService _emailNotificationService;

        public ApplicationChangedHandler(IEmailNotificationService emailNotificationService)
        {
            _emailNotificationService = emailNotificationService;
        }

        public async Task HandleEventAsync(ApplicationChangedEvent eventData)
        {
            
            switch (eventData.Action)
            {
                case GrantApplicationAction.Approve:
                    {
                        await _emailNotificationService.SendEmailNotification(eventData.ApplicationId, _emailNotificationService.GetApprovalBody(), "Grant Application Update");
                        break;
                    }
                case GrantApplicationAction.Deny:
                    {
                        await _emailNotificationService.SendEmailNotification(eventData.ApplicationId, _emailNotificationService.GetDeclineBody(), "Grant Application Update");
                        break;
                    }
                default: break;
            }
        }
    }
}
