using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Modules.Shared.Http
{
    public interface IResilientHttpRequest : IDisposable
    {
        /// <summary>
        /// Set the base URL for relative resource paths.
        /// </summary>
        /// <param name="baseUrl">The base URL to use for relative paths</param>
        void SetBaseUrl(string baseUrl);

        /// <summary>
        /// Send an HTTP request with standard resilience policies applied.
        /// Default timeout: 60 seconds, Max retries: 3
        /// </summary>
        /// <param name="httpVerb">HTTP method to use</param>
        /// <param name="resource">Resource path (absolute or relative to base URL)</param>
        /// <param name="body">Optional request body</param>
        /// <param name="authToken">Optional Bearer token for authorization</param>
        /// <param name="basicAuth">Optional Basic authentication credentials</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HTTP response message</returns>
        Task<HttpResponseMessage> HttpAsync(
            HttpMethod httpVerb,
            string resource,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a long-lived HTTP request with extended timeout and resilience policies applied.
        /// Default timeout: 3 minutes, Max retries: 2
        /// Suitable for operations that may take extended time such as large data processing or report generation.
        /// </summary>
        /// <param name="httpVerb">HTTP method to use</param>
        /// <param name="resource">Resource path (absolute or relative to base URL)</param>
        /// <param name="body">Optional request body</param>
        /// <param name="authToken">Optional Bearer token for authorization</param>
        /// <param name="basicAuth">Optional Basic authentication credentials</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HTTP response message</returns>
        Task<HttpResponseMessage> HttpLongLivedAsync(
            HttpMethod httpVerb,
            string resource,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default);
    }
}