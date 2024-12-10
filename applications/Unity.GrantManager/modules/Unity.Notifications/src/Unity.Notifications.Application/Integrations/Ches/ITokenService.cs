using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Integrations.Ches
{
    public interface ITokenService : IApplicationService
    {
        Task<string> GetAuthTokenAsync();
    }
}
