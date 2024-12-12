using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Modules.Integrations
{
    public interface ITokenService : IApplicationService
    {
        Task<string> GetAuthTokenAsync(ClientOptions clientOptions);
    }
}
