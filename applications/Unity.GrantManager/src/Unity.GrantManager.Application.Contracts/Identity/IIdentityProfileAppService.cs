using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Identity
{
    public interface IIdentityProfileAppService : IApplicationService
    {
        Task CreateOrUpdateAsync();
    }
}
