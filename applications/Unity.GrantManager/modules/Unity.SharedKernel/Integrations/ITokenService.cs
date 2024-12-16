using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Modules.Shared.Integrations
{
    public interface ITokenService : IApplicationService
    {
        Task<string> GetAuthTokenAsync(ClientOptions clientOptions);
    }
}
