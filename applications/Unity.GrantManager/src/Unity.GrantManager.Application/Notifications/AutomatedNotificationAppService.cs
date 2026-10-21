using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.Permissions;
using Unity.Notifications.Templates;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Notifications
{
    [Authorize]
    public class AutomatedNotificationAppService(
        IRepository<ScheduledNotification, Guid> repository,
        ITemplatesRepository templatesRepository,
        IUnitOfWorkManager unitOfWorkManager) : ApplicationService, IAutomatedNotificationAppService
    {
        private readonly IRepository<ScheduledNotification, Guid> _repository = repository;

        [Authorize(NotificationsPermissions.Email.ScheduleCreate)]
        public async Task<NotificationDto> CreateAsync(CreateUpdateNotificationDto input)
        {
            using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            if (await templatesRepository.LockActiveAsync(input.EmailTemplateId) == null)
            {
                throw new BusinessException("Notifications:TemplateNotAvailable");
            }

            var entity = new ScheduledNotification
            {
                FormId = input.FormId,
                EmailTemplateId = input.EmailTemplateId,
                TriggerType = input.TriggerType,
                Module = input.Module,
                TriggerDetail = input.TriggerDetail,
                IsActive = input.IsActive,
                EventType = input.EventType,
                ApplicationStatusId = input.ApplicationStatusId,
                ApplicationStatusIds = input.ApplicationStatusIds,
                ApplicationStatus = input.ApplicationStatus,
                DateField = input.DateField,
                RecipientCategory = input.RecipientCategory,
                RecipientIdentifier = input.RecipientIdentifier
            };

            await _repository.InsertAsync(entity, autoSave: true);
            await uow.CompleteAsync();

            return new NotificationDto
            {
                Id = entity.Id,
                FormId = entity.FormId,
                EmailTemplateId = entity.EmailTemplateId,
                TemplateName = null,
                TriggerType = entity.TriggerType,
                Module = entity.Module,
                TriggerDetail = entity.TriggerDetail,
                IsActive = entity.IsActive,
                EventType = entity.EventType,
                ApplicationStatusId = entity.ApplicationStatusId,
                ApplicationStatusIds = entity.ApplicationStatusIds,
                ApplicationStatus = entity.ApplicationStatus,
                DateField = entity.DateField
            };
        }

        [Authorize(NotificationsPermissions.Email.ScheduleCancel)]
        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id, autoSave: true);
        }

        [Authorize(NotificationsPermissions.Email.ScheduleCancel)]
        public async Task CancelAsync(Guid id)
        {
            var entity = await _repository.GetAsync(id);
            entity.IsActive = false;
            await _repository.UpdateAsync(entity, autoSave: true);
        }

        [Authorize(NotificationsPermissions.Email.NotificationsTab)]
        public async Task<NotificationDto> GetAsync(Guid id)
        {
            var e = await _repository.GetAsync(id);
            return new NotificationDto
            {
                Id = e.Id,
                FormId = e.FormId,
                EmailTemplateId = e.EmailTemplateId,
                TemplateName = null,
                TriggerType = e.TriggerType,
                Module = e.Module,
                TriggerDetail = e.TriggerDetail,
                IsActive = e.IsActive,
                EventType = e.EventType,
                ApplicationStatusId = e.ApplicationStatusId,
                ApplicationStatusIds = e.ApplicationStatusIds,
                ApplicationStatus = e.ApplicationStatus,
                DateField = e.DateField,
                RecipientCategory = e.RecipientCategory,
                RecipientIdentifier = e.RecipientIdentifier
            };
        }

        [Authorize(NotificationsPermissions.Email.NotificationsTab)]
        public async Task<PagedResultDto<NotificationDto>> GetListAsync(GetNotificationsInput input)
        {
            var query = await _repository.GetQueryableAsync();
            if (input.FormId.HasValue)
            {
                query = query.Where(x => x.FormId == input.FormId.Value);
            }
            // Filtering by template name not supported here because templates are stored in the Notifications module.

            var total = await AsyncExecuter.CountAsync(query);

            var list = await query
                .OrderByDescending(x => x.CreationTime)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .ToListAsync();

            var items = list.Select(e => new NotificationDto
            {
                Id = e.Id,
                FormId = e.FormId,
                EmailTemplateId = e.EmailTemplateId,
                TemplateName = null,
                TriggerType = e.TriggerType,
                Module = e.Module,
                TriggerDetail = e.TriggerDetail,
                IsActive = e.IsActive,
                EventType = e.EventType,
                ApplicationStatusId = e.ApplicationStatusId,
                ApplicationStatusIds = e.ApplicationStatusIds,
                ApplicationStatus = e.ApplicationStatus,
                DateField = e.DateField,
                RecipientCategory = e.RecipientCategory,
                RecipientIdentifier = e.RecipientIdentifier
            }).ToList();

            return new PagedResultDto<NotificationDto>(total, items);
        }

        // Limited lookup for template editors (delete/edit preflights); does not expose the schedule list.
        [Authorize(NotificationsPermissions.Email.Send.Default)]
        public async Task<List<TemplateNotificationReferenceDto>> GetTemplateReferencesAsync(Guid templateId)
        {
            var query = await _repository.GetQueryableAsync();
            return await AsyncExecuter.ToListAsync(query
                .Where(x => x.EmailTemplateId == templateId)
                .OrderByDescending(x => x.CreationTime)
                .Select(x => new TemplateNotificationReferenceDto
                {
                    TriggerType = x.TriggerType,
                    TriggerDetail = x.TriggerDetail,
                    IsActive = x.IsActive
                }));
        }

        [Authorize(NotificationsPermissions.Email.ScheduleCreate)]
        public async Task<NotificationDto> UpdateAsync(Guid id, CreateUpdateNotificationDto input)
        {
            using var uow = unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            var e = await _repository.GetAsync(id);
            if (await templatesRepository.LockActiveAsync(input.EmailTemplateId) == null)
            {
                throw new BusinessException("Notifications:TemplateNotAvailable");
            }

            e.EmailTemplateId = input.EmailTemplateId;
            e.TriggerType = input.TriggerType;
            e.Module = input.Module;
            e.TriggerDetail = input.TriggerDetail;
            e.IsActive = input.IsActive;
            e.EventType = input.EventType;
            e.ApplicationStatusId = input.ApplicationStatusId;
            e.ApplicationStatusIds = input.ApplicationStatusIds;
            e.ApplicationStatus = input.ApplicationStatus;
            e.DateField = input.DateField;
            e.RecipientCategory = input.RecipientCategory;
            e.RecipientIdentifier = input.RecipientIdentifier;

            await _repository.UpdateAsync(e, autoSave: true);
            await uow.CompleteAsync();

            return new NotificationDto
            {
                Id = e.Id,
                FormId = e.FormId,
                EmailTemplateId = e.EmailTemplateId,
                TemplateName = null,
                TriggerType = e.TriggerType,
                Module = e.Module,
                TriggerDetail = e.TriggerDetail,
                IsActive = e.IsActive,
                EventType = e.EventType,
                ApplicationStatusId = e.ApplicationStatusId,
                ApplicationStatusIds = e.ApplicationStatusIds,
                ApplicationStatus = e.ApplicationStatus,
                DateField = e.DateField
            };
        }


    }
}
