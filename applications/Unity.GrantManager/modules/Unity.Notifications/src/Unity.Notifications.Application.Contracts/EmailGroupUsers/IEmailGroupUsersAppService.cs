using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Unity.Notifications.EmailGroups
{
    public interface IEmailGroupUsersAppService
    {
        Task<EmailGroupUsersDto> InsertAsync (EmailGroupUsersDto dto);
        Task<bool> DeleteUserAsync (Guid id);
        Task<bool> DeleteUsersByGroupIdAsync (Guid id);
        Task<bool> DeleteUsersByUserIdAsync(Guid id);
        Task<List<EmailGroupUsersDto>> GetEmailGroupUsersByGroupIdAsync(Guid id);
        Task<List<EmailGroupUsersDto>> GetListAsync();
    }
}
