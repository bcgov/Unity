using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Unity.GrantManager.Notifications
{
    public class AutomatedNotificationAppService(IRepository<ScheduledNotification, Guid> repository) : ApplicationService, IAutomatedNotificationAppService
    {
        private readonly IRepository<ScheduledNotification, Guid> _repository = repository;

        public async Task<NotificationDto> CreateAsync(CreateUpdateNotificationDto input)
        {
            var entity = new ScheduledNotification
            {
                FormId = input.FormId,
                EmailTemplateId = input.EmailTemplateId,
                TriggerType = input.TriggerType,
                TriggerDetail = input.TriggerDetail,
                IsActive = input.IsActive,
                EventType = input.EventType,
                ApplicationStatusId = input.ApplicationStatusId,
                ApplicationStatus = input.ApplicationStatus,
                DateField = input.DateField,
                IsDeleted = false
            };

            await _repository.InsertAsync(entity, autoSave: true);

            return new NotificationDto
            {
                Id = entity.Id,
                FormId = entity.FormId,
                EmailTemplateId = entity.EmailTemplateId,
                TemplateName = null,
                TriggerType = entity.TriggerType,
                TriggerDetail = entity.TriggerDetail,
                IsActive = entity.IsActive,
                EventType = entity.EventType,
                ApplicationStatusId = entity.ApplicationStatusId,
                ApplicationStatus = entity.ApplicationStatus,
                DateField = entity.DateField
            };
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id, autoSave: true);
        }

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
                TriggerDetail = e.TriggerDetail,
                IsActive = e.IsActive,
                EventType = e.EventType,
                ApplicationStatusId = e.ApplicationStatusId,
                ApplicationStatus = e.ApplicationStatus,
                DateField = e.DateField
            };
        }

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
                TriggerDetail = e.TriggerDetail,
                IsActive = e.IsActive,
                EventType = e.EventType,
                ApplicationStatusId = e.ApplicationStatusId,
                ApplicationStatus = e.ApplicationStatus,
                DateField = e.DateField
            }).ToList();

            return new PagedResultDto<NotificationDto>(total, items);
        }

        public async Task<NotificationDto> UpdateAsync(Guid id, CreateUpdateNotificationDto input)
        {
            var e = await _repository.GetAsync(id);
            e.EmailTemplateId = input.EmailTemplateId;
            e.TriggerType = input.TriggerType;
            e.TriggerDetail = input.TriggerDetail;
            e.IsActive = input.IsActive;
            e.EventType = input.EventType;
            e.ApplicationStatusId = input.ApplicationStatusId;
            e.ApplicationStatus = input.ApplicationStatus;
            e.DateField = input.DateField;

            await _repository.UpdateAsync(e, autoSave: true);

            return new NotificationDto
            {
                Id = e.Id,
                FormId = e.FormId,
                EmailTemplateId = e.EmailTemplateId,
                TemplateName = null,
                TriggerType = e.TriggerType,
                TriggerDetail = e.TriggerDetail,
                IsActive = e.IsActive,
                EventType = e.EventType,
                ApplicationStatusId = e.ApplicationStatusId,
                ApplicationStatus = e.ApplicationStatus,
                DateField = e.DateField
            };
        }

        public Task<NotificationTemplateDto[]> GetTemplatesAsync()
        {
            // For now return demo templates; replace with tenant-settings-backed templates later
            var templates = new[]
            {
                new NotificationTemplateDto { Name = "WelcomeTemplate", Subject = "Welcome", Body = "Hello {{applicantName}}" },
                new NotificationTemplateDto { Name = "ReminderTemplate", Subject = "Reminder", Body = "Reminder for {{applicationId}}" }
            };
            return Task.FromResult(templates);
        }
    }
}
