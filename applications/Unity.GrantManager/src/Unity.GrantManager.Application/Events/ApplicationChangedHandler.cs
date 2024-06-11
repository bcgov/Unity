using System;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Notifications.EmailNotifications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.Features;

namespace Unity.GrantManager.Events
{
    internal class ApplicationChangedHandler : ILocalEventHandler<ApplicationChangedEvent>, ITransientDependency
    {

        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IApplicantAgentRepository _applicantAgentRepository;
        private readonly IFeatureChecker _featureChecker;

        public ApplicationChangedHandler(IEmailNotificationService emailNotificationService,
            IApplicantAgentRepository applicantAgentRepository,
            IFeatureChecker featureChecker)
        {
            _emailNotificationService = emailNotificationService;
            _applicantAgentRepository = applicantAgentRepository;
            _featureChecker = featureChecker;
        }

        public async Task HandleEventAsync(ApplicationChangedEvent eventData)
        {
            if (await _featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                await EmailNotificationEventAsync(eventData);
            }
        }

        private async Task EmailNotificationEventAsync(ApplicationChangedEvent eventData)
        {
            var applicantAgent = await _applicantAgentRepository.FirstOrDefaultAsync(a => a.ApplicationId == eventData.ApplicationId);
            if (applicantAgent == null) return;

            string email = applicantAgent.Email;
            string subject = "Grant Application Update";

            if (!string.IsNullOrEmpty(email))
            {
                switch (eventData.Action)
                {
                    case GrantApplicationAction.Approve:
                        {
                            await _emailNotificationService.SendEmaiToQueue(
                                email, _emailNotificationService.GetApprovalBody(), 
                                subject, 
                                eventData.ApplicationId);
                            break;
                        }
                    case GrantApplicationAction.Deny:
                        {
                            await _emailNotificationService.SendEmaiToQueue(
                                email, 
                                _emailNotificationService.GetDeclineBody(),
                                subject, 
                                eventData.ApplicationId);
                            break;
                        }
                    default: break;
                }
            }
        }
    }
}
