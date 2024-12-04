using System;
using System.Threading.Tasks;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.Features;

namespace Unity.GrantManager.Events
{
    internal class EmailNotificationHandler : ILocalEventHandler<EmailNotificationEvent>, ITransientDependency
    {

        private readonly IEmailNotificationService _emailNotificationService;
        private readonly IFeatureChecker _featureChecker;

        private const string GRANT_APPLICATION_UPDATE_SUBJECT = "Grant Application Update";
        private const string FAILED_PAYMENTS_SUBJECT = "Failed Payment Requests";

        public EmailNotificationHandler(
            IEmailNotificationService emailNotificationService,
            IFeatureChecker featureChecker)
        {
            _emailNotificationService = emailNotificationService;
            _featureChecker = featureChecker;
        }

        public async Task HandleEventAsync(EmailNotificationEvent eventData)
        {
            if (await _featureChecker.IsEnabledAsync("Unity.Notifications"))
            {
                await EmailNotificationEventAsync(eventData);
            }
        }


        private async Task InitializeAndSendEmailToQueue(string emailTo, string body, string subject, Guid applicationId, string? emailFrom)
        {
            EmailLog emailLog = await _emailNotificationService.InitializeEmailLog(
                                                emailTo,
                                                body,
                                                subject,
                                                applicationId, 
                                                emailFrom) ?? throw new UserFriendlyException("Unable to Initialize Email Log");

            await _emailNotificationService.SendEmailToQueue(emailLog);
        }

        private async Task EmailNotificationEventAsync(EmailNotificationEvent eventData)
        {
            if (eventData == null) return;

            string emailTo = eventData.EmailAddress;
            switch (eventData.Action)
            {
                case EmailAction.SendFailedSummary:
                    {
                        foreach(string emailToAddress in eventData.EmailAddressList)
                        {
                            await InitializeAndSendEmailToQueue(emailToAddress, eventData.Body, FAILED_PAYMENTS_SUBJECT, eventData.ApplicationId, eventData.EmailFrom);
                        }
  
                        break;
                    }
                case EmailAction.SendApproval:
                    {
                        string body = _emailNotificationService.GetApprovalBody();
                        await InitializeAndSendEmailToQueue(emailTo, body, GRANT_APPLICATION_UPDATE_SUBJECT, eventData.ApplicationId, eventData.EmailFrom);
                        break;
                    }
                case EmailAction.SendDecline:
                    {
                        string body = _emailNotificationService.GetDeclineBody();
                        await InitializeAndSendEmailToQueue(emailTo, body, GRANT_APPLICATION_UPDATE_SUBJECT, eventData.ApplicationId, eventData.EmailFrom);
                        break;
                    }
                case EmailAction.SendCustom:
                    {
                        foreach (string emailToAddress in eventData.EmailAddressList)
                        {
                            await InitializeAndSendEmailToQueue(emailToAddress, eventData.Body, eventData.Subject, eventData.ApplicationId, eventData.EmailFrom);
                        }
                        
                        break;
                    }
                case EmailAction.Retry:
                    break;
                default: break;
            }
        }                       
        
    }
}
