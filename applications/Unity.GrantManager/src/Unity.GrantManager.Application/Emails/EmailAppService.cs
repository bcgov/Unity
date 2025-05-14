using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
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
            return
            new EmailNotificationEvent
            {
                Id = dto.EmailId,
                ApplicationId = dto.ApplicationId,
                RetryAttempts = 0,
                EmailAddress = dto.EmailTo,
                EmailAddressList = toList,
                EmailFrom = dto.EmailFrom,
                Subject = dto.EmailSubject,
                Body = dto.EmailBody,
                EmailTemplateName = dto.EmailTemplateName
            };
        }
    }
}