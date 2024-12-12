using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.Modules.Http
{
    public interface IResilientHttpRequest : IRemoteService
    {
        Task<HttpResponseMessage> HttpAsyncWithBody(HttpMethod httpVerb, string resource, string? body = null, string? authToken = null);
        Task<HttpResponseMessage> HttpAsync(HttpMethod httpVerb, string resource, string? authToken = null);
    }
}
