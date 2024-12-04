using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;

namespace Unity.GrantManager.Events
{
    internal class ApplicationChangedHandler : ILocalEventHandler<ApplicationChangedEvent>, ITransientDependency
    {
        private readonly IApplicantAgentRepository _applicantAgentRepository;
        private readonly IFeatureChecker _featureChecker;
        private readonly ILocalEventBus _localEventBus;
        private readonly IConfiguration _configuration;

        public ApplicationChangedHandler(
            IConfiguration configuration,
            IApplicantAgentRepository applicantAgentRepository,
            ILocalEventBus localEventBus,
            IFeatureChecker featureChecker)
        {
            _configuration = configuration;
            _applicantAgentRepository = applicantAgentRepository;
            _localEventBus = localEventBus;
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

            string? emailTo = applicantAgent.Email;
            string emailFrom =_configuration["Notifications:ChesFromEmail"] ?? "unity@gov.bc.ca";

            if (!string.IsNullOrEmpty(emailTo))
            {
                switch (eventData.Action)
                {
                    case GrantApplicationAction.Approve:
                        {
                            await _localEventBus.PublishAsync(
                                new EmailNotificationEvent
                                {
                                    Action = EmailAction.SendApproval,
                                    ApplicationId = eventData.ApplicationId,
                                    RetryAttempts = 0,
                                    EmailAddress = emailTo,
                                    EmailFrom = emailFrom
                                }
                            );
                            break;
                        }
                    case GrantApplicationAction.Deny:
                        {
                            await _localEventBus.PublishAsync(
                                new EmailNotificationEvent
                                {
                                    Action = EmailAction.SendDecline,
                                    ApplicationId = eventData.ApplicationId,
                                    RetryAttempts = 0,
                                    EmailAddress = emailTo,
                                    EmailFrom = emailFrom
                                }
                            );
                            break;
                        }
                    default: break;
                }
            }
        }
    }
}
