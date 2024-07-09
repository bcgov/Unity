using RestSharp;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Integrations.Ches
{
    public interface IChesClientService : IApplicationService
    {
        Task<RestResponse> SendAsync(object emailRequest);

        Task<RestResponse> HealthCheckAsync();
    }
}
