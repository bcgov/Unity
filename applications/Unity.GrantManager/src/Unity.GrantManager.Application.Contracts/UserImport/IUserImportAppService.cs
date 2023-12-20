using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.UserImport
{
    public interface IUserImportAppService
    {
        Task ImportUserAsync(ImportUserDto importUserDto);
        Task<IList<UserDto>> SearchAsync(UserSearchDto importUserSearchDto);
    }
}
