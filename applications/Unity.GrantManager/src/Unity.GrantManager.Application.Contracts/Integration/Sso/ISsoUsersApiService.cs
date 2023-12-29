using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integration.Sso
{
    public interface ISsoUsersApiService : IApplicationService
    {
        Task<UserSearchResult> FindUserAsync(string directory, string guid);
        Task<UserSearchResult> SearchUsersAsync(string directory, string? firstName = null, string? lastName = null);
    }
}
