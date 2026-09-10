using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Notifications.Email;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.Notifications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EmailAppService), typeof(IEmailAppService))]
    public class EmailAppService(
        ILocalEventBus localEventBus,
        IEmailNotificationService emailNotificationService,
        EmailAttachmentService emailAttachmentService) : ApplicationService, IEmailAppService
    {
        public async Task<Guid> InitializeDraftAsync(Guid applicationId)
        {
            return await emailNotificationService.InitializeDraftAsync(applicationId);
        }
        public async Task<bool> SendAsync(CreateEmailDto dto)
        {
            if (dto.EmailId != Guid.Empty)
            {
                // Validate at the HTTP application-service boundary so ABP serializes the
                // user-friendly missing-file message directly back to the email composer.
                // The event, queue, and worker checks remain as race-condition defenses.
                await emailAttachmentService.ValidateEmailAttachmentsAsync(dto.EmailId);
            }

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

        private EmailNotificationEvent GetEmailNotificationEvent(CreateEmailDto dto)
        {
            var toList = dto.EmailTo.ParseEmailList() ?? [];
            var ccList = dto.EmailCC.ParseEmailList() ?? [];
            var bccList = dto.EmailBCC.ParseEmailList() ?? [];

            return new EmailNotificationEvent
            {
                Id = dto.EmailId,
                TenantId = CurrentTenant.Id,
                ApplicationId = dto.ApplicationId,
                RetryAttempts = 0,
                EmailAddress = dto.EmailTo,
                EmailAddressList = toList,
                EmailFrom = dto.EmailFrom,
                Cc = ccList,
                Bcc = bccList,
                Subject = dto.EmailSubject,
                Body = dto.EmailBody,
                TemplateId = dto.TemplateId ?? Guid.Empty,
                EmailTemplateName = dto.EmailTemplateName,
                SendOnDateTime = dto.SendOnDateTime
            };
        }
    }
}
