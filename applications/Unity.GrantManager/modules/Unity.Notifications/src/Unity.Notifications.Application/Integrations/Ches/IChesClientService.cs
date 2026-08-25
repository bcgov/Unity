using System;
using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Integrations.Ches
{    
    public interface IChesClientService : IApplicationService
    {
        Task<HttpResponseMessage?> SendAsync(object emailRequest);
        Task<HttpResponseMessage?> GetStatusAsync(Guid messageId);
        Task<HttpResponseMessage?> CancelEmailAsync(Guid messageId);
    }
}

