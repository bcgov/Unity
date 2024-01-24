using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Identity
{
    public interface IUserImportAppService : IApplicationService
    {
        Task ImportUserAsync(ImportUserDto importUserDto);
        Task<IList<UserDto>> SearchAsync(UserSearchDto importUserSearchDto);
    }
}
