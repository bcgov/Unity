using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.Notifications.Email;
using Unity.Modules.Shared.Utils;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Events;
using Unity.Notifications.Permissions;
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
        EmailAttachmentService emailAttachmentService,
        EmailComposerAccessChecker emailAccessChecker,
        EmailNotificationManager emailNotificationManager) : ApplicationService, IEmailAppService
    {
        [Authorize(NotificationsPermissions.Email.Default)]
        public async Task<Guid> InitializeDraftAsync(Guid applicationId)
        {
            await emailAccessChecker.CheckOwnerAsync(applicationId, Guid.Empty, EmailOperation.Create);
            return await emailNotificationService.InitializeDraftAsync(applicationId);
        }

        [Authorize(NotificationsPermissions.Email.Default)]
        public virtual async Task<Guid> InitializeApplicantDraftAsync(Guid applicantId)
        {
            await emailAccessChecker.CheckOwnerAsync(Guid.Empty, applicantId, EmailOperation.Create);
            var email = await emailNotificationManager.CreateDraftEmailLogAsync(Guid.Empty, applicantId);
            return email.Id;
        }

        [Authorize(NotificationsPermissions.Email.Default)]
        public async Task<bool> SendAsync(CreateEmailDto dto)
        {
            await ValidateSendRequestAsync(dto);
            if (dto.SendOnDateTime.HasValue)
            {
                await emailAccessChecker.CheckScheduleAsync(dto.ApplicantId ?? Guid.Empty);
            }
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

        [Authorize(NotificationsPermissions.Email.Default)]
        public async Task<bool> SaveDraftAsync(CreateEmailDto dto)
        {
            var applicantId = dto.ApplicantId ?? Guid.Empty;
            if (dto.EmailId == Guid.Empty)
            {
                await emailAccessChecker.CheckOwnerAsync(dto.ApplicationId, applicantId, EmailOperation.Create);
            }
            else
            {
                var email = await emailAccessChecker.CheckDraftEditAsync(dto.EmailId);
                EmailOwnership.EnsureDraftOwner(email, dto.ApplicationId, applicantId);
                dto.TemplateId = ResolveTemplateId(dto, email);
            }
            await ValidateTemplateAsync(dto, applicantId);
            if (dto.SendOnDateTime.HasValue)
            {
                throw new Volo.Abp.BusinessException("Notifications:EmailDraftCannotBeScheduled");
            }
            EmailNotificationEvent emailNotificationEvent = GetEmailNotificationEvent(dto);
            emailNotificationEvent.Action = EmailAction.SaveDraft;
            await localEventBus.PublishAsync(emailNotificationEvent);
            return true;
        }

        private async Task ValidateSendRequestAsync(CreateEmailDto dto)
        {
            var applicantId = dto.ApplicantId ?? Guid.Empty;
            if (dto.EmailId == Guid.Empty)
            {
                await emailAccessChecker.CheckOwnerAsync(dto.ApplicationId, applicantId, EmailOperation.Create);
                await emailAccessChecker.CheckOwnerAsync(dto.ApplicationId, applicantId, EmailOperation.Send);
            }
            else
            {
                var email = await emailAccessChecker.CheckEmailAsync(dto.EmailId, EmailOperation.Send, requireDraft: true);
                EmailOwnership.EnsureDraftOwner(email, dto.ApplicationId, applicantId);
                dto.TemplateId = ResolveTemplateId(dto, email);
                if (!await emailAccessChecker.CanEditDraftAsync(email))
                {
                    // Without edit rights the draft goes out exactly as it was saved.
                    ApplySavedContent(dto, email);
                }
            }
            await ValidateTemplateAsync(dto, applicantId);
        }

        private static Guid? ResolveTemplateId(CreateEmailDto dto, EmailLog email)
        {
            return !dto.TemplateId.HasValue || dto.TemplateId == Guid.Empty
                ? EmailOwnership.GetTemplateId(email)
                : dto.TemplateId;
        }

        private static void ApplySavedContent(CreateEmailDto dto, EmailLog email)
        {
            dto.EmailTo = email.ToAddress;
            dto.EmailFrom = email.FromAddress;
            dto.EmailCC = email.CC;
            dto.EmailBCC = email.BCC;
            dto.EmailSubject = email.Subject;
            dto.EmailBody = email.Body;
        }

        private async Task ValidateTemplateAsync(CreateEmailDto dto, Guid applicantId)
        {
            if (dto.TemplateId.HasValue && dto.TemplateId.Value != Guid.Empty)
            {
                var template = await emailAccessChecker.CheckTemplateAsync(dto.TemplateId.Value, applicantId);
                dto.EmailTemplateName = template.Name;
            }
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
                ApplicantId = dto.ApplicantId ?? Guid.Empty,
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
