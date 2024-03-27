using Polly.CircuitBreaker;
using Polly.Retry;
using RestSharp;
using RestSharp.Authenticators;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Notifications.Integrations.Http
{
    public interface IResilientHttpRequest : IApplicationService
    {
        Task<RestResponse> HttpAsync(Method httpVerb, string resource, Dictionary<string, string>? headers = null, object? requestObject = null, IAuthenticator? authenticator = null);
        Task<RestResponse> HttpAsync(Method httpVerb, string resource, AsyncRetryPolicy<RestResponse> retryPolicy, Dictionary<string, string>? headers = null, object? requestObject = null, IAuthenticator? authenticator = null);
        Task<RestResponse> HttpAsync(Method httpVerb, string resource, AsyncRetryPolicy<RestResponse> retryPolicy, AsyncCircuitBreakerPolicy<RestResponse> circuitBreakerPolicy, Dictionary<string, string>? headers = null, object? requestObject = null, IAuthenticator? authenticator = null);
    }
}
