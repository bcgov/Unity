using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Identity
{
    public interface IUserImportAppService : IApplicationService
    {
        Task AutoImportUserInternalAsync(ImportUserDto importUserDto, string username, string firstName, string lastName, string emailAddress, string oidcSub, string displayName);
        Task ImportUserAsync(ImportUserDto importUserDto);
        Task<IList<UserDto>> SearchAsync(UserSearchDto importUserSearchDto);
    }
}
