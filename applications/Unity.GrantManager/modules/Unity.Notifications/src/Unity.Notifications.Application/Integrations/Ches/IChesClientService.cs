using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Integrations.Ches
{
    [RemoteService(Name = "Ches")]
    public interface IChesClientService : IApplicationService
    {
        Task<HttpResponseMessage?> SendAsync(object emailRequest);
    }
}

