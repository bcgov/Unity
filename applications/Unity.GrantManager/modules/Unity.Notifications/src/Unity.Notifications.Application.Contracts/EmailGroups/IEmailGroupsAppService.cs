using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace Unity.Notifications.EmailGroups
{
    public interface IEmailGroupsAppService
    {
        Task<EmailGroupDto> CreateAsync (EmailGroupDto dto);
        Task<EmailGroupDto> UpdateAsync (EmailGroupDto dto);
        Task<bool> DeleteAsync (Guid id);
        [AllowAnonymous]
        Task<List<EmailGroupDto>> GetListAsync();
        Task<EmailGroupDto> GetEmailGroupByIdAsync(Guid id);
    }
}
