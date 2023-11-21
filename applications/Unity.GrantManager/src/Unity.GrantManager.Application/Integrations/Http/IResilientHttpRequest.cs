using Polly.CircuitBreaker;
using Polly.Retry;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Http
{
    public interface IResilientHttpRequest : IApplicationService
    {
        Task<RestResponse> HttpAsync(Method httpVerb, string resource, Dictionary<string, string>? headers = null, object? requestObject = null);
        Task<RestResponse> HttpAsync(Method httpVerb, string resource, RetryPolicy<RestResponse> retryPolicy, Dictionary<string, string>? headers = null, object? requestObject = null);
        Task<RestResponse> HttpAsync(Method httpVerb, string resource, RetryPolicy<RestResponse> retryPolicy, CircuitBreakerPolicy<RestResponse> circuitBreakerPolicy, Dictionary<string, string>? headers = null, object? requestObject = null);
    }
}
