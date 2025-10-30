using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.Modules.Shared.Http
{
    public interface IResilientHttpRequest : IRemoteService
    {
        /// <summary>
        /// Send an HTTP request with optional JSON body, authentication, and resilience policies.
        /// If body is an object, it will be automatically serialized to JSON.
        /// </summary>
        Task<HttpResponseMessage> HttpAsync(
            HttpMethod httpVerb,
            string resource,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Set a base URL to be used for relative request paths.
        /// </summary>
        void SetBaseUrl(string baseUrl);
        
        Task<HttpResponseMessage> HttpAsyncSecured(
            HttpMethod httpVerb,
            string resource,
            string certPath,
            string? certPassword = null,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default);
    }
}