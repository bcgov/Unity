using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Payments.Integrations.Http
{
    public interface IResilientHttpRequest : IApplicationService
    {
        Task<HttpResponseMessage> HttpAsyncWithBody(HttpMethod httpVerb, string resource, string? body = null, string? authToken = null);
        Task<HttpResponseMessage> HttpAsync(HttpMethod httpVerb, string resource, string? authToken = null);
    }
}
