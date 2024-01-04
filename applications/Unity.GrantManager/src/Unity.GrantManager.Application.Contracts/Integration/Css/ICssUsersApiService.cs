using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integration.Css
{
    public interface ICssUsersApiService : IApplicationService
    {
        Task<UserSearchResult> FindUserAsync(string directory, string guid);
        Task<UserSearchResult> SearchUsersAsync(string directory, string? firstName = null, string? lastName = null);
    }
}
