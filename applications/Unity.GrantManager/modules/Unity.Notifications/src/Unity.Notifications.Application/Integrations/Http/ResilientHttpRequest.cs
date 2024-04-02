using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.Notifications.Integrations.Http
{
    [IntegrationService]
    public class ResilientHttpRequest : NotificationsAppService, IResilientHttpRequest
    {
        private readonly RestClient _restClient;
        private static int _maxRetryAttempts = 3;
        private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(10);

        public ResilientHttpRequest(RestClient restClient)
        {
            _restClient = restClient;
        }

        public static void SetMaxRetryAttemptsAndPauseBetweenFailures(int maxRetryAttempts, TimeSpan pauseBetweenFailures)
        {
            _maxRetryAttempts = maxRetryAttempts;
            _pauseBetweenFailures = pauseBetweenFailures;
        }

        public async Task<RestResponse> HttpAsync(Method httpVerb, string resource, Dictionary<string, string>? headers = null, object? requestObject = null, IAuthenticator? authenticator = null)
        {
            return await ExecuteRequestAsync(httpVerb, resource, headers, requestObject, null, null, authenticator);
        }

        public async Task<RestResponse> HttpAsync(Method httpVerb, string resource, AsyncRetryPolicy<RestResponse> retryPolicy, Dictionary<string, string>? headers = null, object? requestObject = null, IAuthenticator? authenticator = null)
        {
            return await ExecuteRequestAsync(httpVerb, resource, headers, requestObject, retryPolicy, null, authenticator);
        }

        public async Task<RestResponse> HttpAsync(Method httpVerb, string resource, AsyncRetryPolicy<RestResponse> retryPolicy, AsyncCircuitBreakerPolicy<RestResponse> circuitBreakerPolicy, Dictionary<string, string>? headers = null, object? requestObject = null, IAuthenticator? authenticator = null)
        {
            return await ExecuteRequestAsync(httpVerb, resource, headers, requestObject, retryPolicy, circuitBreakerPolicy, authenticator);
        }

        private async Task<RestResponse> ExecuteRequestAsync(Method httpVerb, string resource, Dictionary<string, string>? headers, object? requestObject, AsyncRetryPolicy<RestResponse>? retryPolicy = null, AsyncCircuitBreakerPolicy<RestResponse>? circuitBreakerPolicy = null, IAuthenticator? authenticator = null)
        {
            RestResponse? restResponse;

            try
            {
                var restRequest = new RestRequest(resource, httpVerb);
                if (authenticator != null)
                {
                    restRequest.Authenticator = authenticator;
                }
                restRequest.AddHeader("cache-control", "no-cache");
                if (headers != null && headers.Count > 0)
                    foreach (var header in headers)
                        restRequest.AddHeader(header.Key, header.Value);

                if (httpVerb != Method.Get && requestObject != null)
                {
                    restRequest.RequestFormat = DataFormat.Json;
                    restRequest.AddJsonBody(requestObject);
                }
                restResponse = await RestResponseWithPolicyAsync(restRequest, retryPolicy, circuitBreakerPolicy);
            }
            catch (Exception ex)
            {
                restResponse = new RestResponse
                {
                    Content = ex.Message,
                    ErrorMessage = ex.Message,
                    ResponseStatus = ResponseStatus.TimedOut,
                    StatusCode = HttpStatusCode.ServiceUnavailable
                };
            }

            return await Task.FromResult(restResponse);
        }

        private async Task<RestResponse> RestResponseWithPolicyAsync(RestRequest restRequest, AsyncRetryPolicy<RestResponse>? retryPolicy = null, AsyncCircuitBreakerPolicy<RestResponse>? circuitBreakerPolicy = null)
        {
            retryPolicy ??= Policy
                    .HandleResult<RestResponse>(x => !x.IsSuccessful)
                    .WaitAndRetryAsync(_maxRetryAttempts, x => _pauseBetweenFailures, async (iRestResponse, timeSpan, retryCount, context) =>
                    {
                        await Task.Run(() => Logger.LogError("The request failed. HttpStatusCode={statusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}. Uri={responseUri}; RequestResponse={responseContent}", iRestResponse.Result.StatusCode, timeSpan, retryCount, iRestResponse.Result.ResponseUri, iRestResponse.Result.Content));
                    });

            circuitBreakerPolicy ??= Policy
                .HandleResult<RestResponse>(x => x.StatusCode == HttpStatusCode.ServiceUnavailable || x.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(30), onBreak: async (iRestResponse, timespan, context) =>
                {
                    await Task.Run(() => Logger.LogError("Circuit went into a fault state. Reason: {resultContent}", iRestResponse.Result.Content));
                },
                onReset: async (context) =>
                {
                    await Task.Run(() => Logger.LogError($"Circuit left the fault state."));
                });

            return await retryPolicy.WrapAsync(circuitBreakerPolicy).ExecuteAsync(async () => await _restClient.ExecuteAsync(restRequest));
        }

    }
}
