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

            List<string> toList = new();
            string[] emails = dto.EmailTo.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);

            foreach (string email in emails) {
                toList.Add(email.Trim());
            }

            await localEventBus.PublishAsync(
                new EmailNotificationEvent
                {
                    Action = EmailAction.SendCustom,
                    ApplicationId = dto.ApplicationId,
                    RetryAttempts = 0,
                    EmailAddress = dto.EmailTo, 
                    EmailAddressList = toList,
                    EmailFrom = dto.EmailFrom,
                    Subject = dto.EmailSubject,
                    Body = dto.EmailBody
                }
            );

            return true;
        }
    }
}