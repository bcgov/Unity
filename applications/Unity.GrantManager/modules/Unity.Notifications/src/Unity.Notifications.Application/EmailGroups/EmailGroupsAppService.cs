using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp;
using Unity.GrantManager.Notifications;


namespace Unity.Notifications.EmailGroups
{

    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EmailGroupsAppService), typeof(IEmailGroupsAppService))]
    public class EmailGroupsAppService : ApplicationService, IEmailGroupsAppService
    {
        private readonly IEmailGroupsRepository _emailGroupsRepository;
        private readonly IRepository<ScheduledNotification, Guid> _scheduledNotificationRepository;

        public EmailGroupsAppService(
            IEmailGroupsRepository emailGroupsRepository,
            IRepository<ScheduledNotification, Guid> scheduledNotificationRepository)
        {
            _emailGroupsRepository = emailGroupsRepository;
            _scheduledNotificationRepository = scheduledNotificationRepository;
        }
        public async Task<EmailGroupDto> CreateAsync(EmailGroupDto dto)
        {
            var newGroup =  await _emailGroupsRepository.InsertAsync(new EmailGroup
            {
                Name = dto.Name,
                Description = dto.Description,
                Type = dto.Type
            });
            return new EmailGroupDto
            {
                Id = newGroup.Id,
                Name = newGroup.Name,
                Description = newGroup.Description,
                Type = newGroup.Type
            };
        }

        public async Task<EmailGroupDto> UpdateAsync(EmailGroupDto dto)
        {
            var emailGroup = await _emailGroupsRepository.GetAsync(dto.Id, true);
            emailGroup.Name = dto.Name;
            emailGroup.Description = dto.Description;
            emailGroup.Type = dto.Type;
            await _emailGroupsRepository.UpdateAsync(emailGroup, autoSave: true);
            return new EmailGroupDto
            {
                Id = emailGroup.Id,
                Name = emailGroup.Name,
                Description = emailGroup.Description,
                Type = emailGroup.Type
            };
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var emailGroup = await _emailGroupsRepository.GetAsync(id);
            if (await IsUsedByScheduledNotificationAsync(emailGroup.Name))
            {
                throw new BusinessException(
                    "Unity.Notifications:EmailGroupInUse",
                    "This email group is associated with a scheduled notification and cannot be deleted.");
            }

            try
            {
                await _emailGroupsRepository.DeleteAsync(id);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting email group with ID {id}: {ex.Message}");
            }
        }

        [AllowAnonymous]
        public async Task<List<EmailGroupDto>> GetListAsync()
        {
            var groups =  await _emailGroupsRepository.GetListAsync();
            var usedGroupNames = await GetScheduledNotificationGroupNamesAsync();
            var groupDtos = ObjectMapper.Map<List<EmailGroup>, List<EmailGroupDto>>(groups);

            foreach (var groupDto in groupDtos)
            {
                groupDto.IsUsedByScheduledNotification = usedGroupNames.Contains(groupDto.Name);
            }

            return groupDtos;
        }

        public async Task<EmailGroupDto> GetEmailGroupByIdAsync(Guid id)
        {
            var group = await _emailGroupsRepository.GetAsync(id);
            return ObjectMapper.Map<EmailGroup, EmailGroupDto>(group);
        }

        private async Task<bool> IsUsedByScheduledNotificationAsync(string groupName)
        {
            var usedGroupNames = await GetScheduledNotificationGroupNamesAsync();
            return usedGroupNames.Contains(groupName);
        }

        private async Task<HashSet<string>> GetScheduledNotificationGroupNamesAsync()
        {
            var notifications = await _scheduledNotificationRepository.GetListAsync();

            return notifications
                .Where(notification => notification.IsActive &&
                    string.Equals(notification.RecipientCategory, "Internal", StringComparison.OrdinalIgnoreCase))
                .SelectMany(notification => (notification.RecipientIdentifier ?? string.Empty)
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

    }
}
