using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Comments;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;


namespace Unity.Notifications.EmailGroups
{

    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EmailGroupsAppService), typeof(IEmailGroupsAppService))]
    public class EmailGroupsAppService : ApplicationService, IEmailGroupsAppService
    {
        private readonly IEmailGroupsRepository _emailGroupsRepository;

        public EmailGroupsAppService(IEmailGroupsRepository emailGroupsRepository)
        {
            _emailGroupsRepository = emailGroupsRepository;
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
           await _emailGroupsRepository.UpdateAsync(emailGroup,autoSave:true);
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
            try
            {
                await _emailGroupsRepository.DeleteAsync(id);
                return true;
            }
            catch(Exception ex)
            {
                throw new Exception($"Error deleting email group with ID {id}: {ex.Message}");
            }
        }

        public async Task<List<EmailGroupDto>> GetListAsync()
        {
            var groups =  await _emailGroupsRepository.GetListAsync();
          return  ObjectMapper.Map<List<EmailGroup>, List<EmailGroupDto>>(groups);
        }

        public async Task<EmailGroupDto> GetEmailGroupByIdAsync(Guid id)
        {
            var group = await _emailGroupsRepository.GetAsync(id);
            return ObjectMapper.Map<EmailGroup,EmailGroupDto>(group);
        }

    }
}
