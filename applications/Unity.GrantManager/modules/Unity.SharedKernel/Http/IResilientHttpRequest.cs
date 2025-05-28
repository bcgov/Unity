using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.Modules.Shared.Http
{
    public interface IResilientHttpRequest : IRemoteService
    {
        Task<HttpResponseMessage> HttpAsyncWithBody(HttpMethod httpVerb, string resource, string? body = null, string? authToken = null);
        Task<HttpResponseMessage> HttpAsync(HttpMethod httpVerb, string resource, string? authToken = null);
        Task<HttpResponseMessage> ExecuteRequestAsync(
            HttpMethod httpVerb,
            string resource,
            string? body,
            string? authToken,
            (string username, string password)? basicAuth = null);

        void SetBaseUrl(string baseUrl);
    }
}
