using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.Emails
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EmailAppService), typeof(IEmailAppService))]
    public class EmailAppService(ILocalEventBus localEventBus) : ApplicationService, IEmailAppService
    {
        public async Task<bool> CreateAsync(CreateEmailDto dto)
        {
            EmailNotificationEvent emailNotificationEvent = GetEmailNotificationEvent(dto);
            emailNotificationEvent.Action = EmailAction.SendCustom;
            await localEventBus.PublishAsync(emailNotificationEvent);
            return true;
        }

        public async Task<bool> SaveDraftAsync(CreateEmailDto dto)
        {
            EmailNotificationEvent emailNotificationEvent = GetEmailNotificationEvent(dto);
            emailNotificationEvent.Action = EmailAction.SaveDraft;
            await localEventBus.PublishAsync(emailNotificationEvent);
            return true;
        }

        private static EmailNotificationEvent GetEmailNotificationEvent(CreateEmailDto dto)
        {
            List<string> toList = [];
            string[] emails = dto.EmailTo.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);

            foreach (string email in emails)
            {
                toList.Add(email.Trim());
            }

            // Parse CC emails
            IEnumerable<string> ccList = [];
            if (!string.IsNullOrWhiteSpace(dto.EmailCC))
            {
                ccList = dto.EmailCC.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                                   .Select(email => email.Trim())
                                   .Where(email => !string.IsNullOrWhiteSpace(email));
            }

            // Parse BCC emails
            IEnumerable<string> bccList = [];
            if (!string.IsNullOrWhiteSpace(dto.EmailBCC))
            {
                bccList = dto.EmailBCC.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                                     .Select(email => email.Trim())
                                     .Where(email => !string.IsNullOrWhiteSpace(email));
            }

            return
            new EmailNotificationEvent
            {
                Id = dto.EmailId,
                ApplicationId = dto.ApplicationId,
                RetryAttempts = 0,
                EmailAddress = dto.EmailTo,
                EmailAddressList = toList,
                EmailFrom = dto.EmailFrom,
                Cc = ccList,
                Bcc = bccList,
                Subject = dto.EmailSubject,
                Body = dto.EmailBody,
                EmailTemplateName = dto.EmailTemplateName
            };
        }
    }
}