using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;


namespace Unity.Notifications.EmailGroups
{

    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(EmailGroupUsersAppService), typeof(IEmailGroupUsersAppService))]
    public class EmailGroupUsersAppService : ApplicationService, IEmailGroupUsersAppService
    {
        private readonly IEmailGroupUsersRepository _emailGroupUsersRepository;

        public EmailGroupUsersAppService(IEmailGroupUsersRepository emailGroupUsersRepository)
        {
            _emailGroupUsersRepository = emailGroupUsersRepository;
        }
        public async Task<EmailGroupUsersDto> InsertAsync(EmailGroupUsersDto dto)
        {
           var newUser =  await _emailGroupUsersRepository.InsertAsync(new EmailGroupUser
           {
                GroupId = dto.GroupId,
                UserId = dto.UserId,
                
            });
            return new EmailGroupUsersDto
            {
                Id = newUser.Id,
                GroupId = newUser.GroupId,
                UserId = newUser.UserId,
            };
        }

       

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            try
            {
                await _emailGroupUsersRepository.DeleteAsync(id);
                return true;
            }
            catch(Exception ex)
            {
                throw new Exception($"Error deleting email group with ID {id}: {ex.Message}");
            }
        }
        public async Task<bool> DeleteUsersByUserIdAsync(Guid id)
        {
            try
            {
                await _emailGroupUsersRepository.DeleteAsync(id);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting email group with ID {id}: {ex.Message}");
            }
        }
        public async Task<bool> DeleteUsersByGroupIdAsync(Guid id)
        {
            try
            {
                await _emailGroupUsersRepository.DeleteAsync(id);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting email group with ID {id}: {ex.Message}");
            }
        }

        public async Task<List<EmailGroupUsersDto>> GetEmailGroupUsersByGroupIdsync(Guid id)
        {
            var users = await _emailGroupUsersRepository.GetListAsync(u => u.GroupId == id);

            return  ObjectMapper.Map<List<EmailGroupUser>, List<EmailGroupUsersDto>>(users);
        }
    }
}
