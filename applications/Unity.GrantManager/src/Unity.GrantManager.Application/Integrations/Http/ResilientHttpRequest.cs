using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.GrantManager.Integrations.Http
{
    [RemoteService(false)]
    public class ResilientHttpRequest : GrantManagerAppService, IResilientHttpRequest
    {
        private readonly RestClient _restClient;
        private static int _maxRetryAttempts = 5;
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

        public async Task<RestResponse> HttpAsync(Method httpVerb, string resource, Dictionary<string, string>? headers = null, object? requestObject = null)
        {
            RestResponse? restResponse;

            try
            {
                var restRequest = new RestRequest(resource, httpVerb);
                restRequest.AddHeader("cache-control", "no-cache");
                if (headers != null && headers.Count > 0)
                    foreach (var header in headers)
                        restRequest.AddHeader(header.Key, header.Value);

                if (httpVerb != Method.Get)
                {
                    object json = new();
                    if (requestObject != null)
                    {
                        json = JsonSerialize(requestObject);
                    }
                    restRequest.RequestFormat = DataFormat.Json;
                    restRequest.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
                }
                restResponse = await RestResponseWithPolicyAsync(restRequest);
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

        private async Task<RestResponse> RestResponseWithPolicyAsync(RestRequest restRequest)
        {
            var retryPolicy = Policy
                .HandleResult<RestResponse>(x => !x.IsSuccessful)
                .WaitAndRetryAsync(_maxRetryAttempts, x => _pauseBetweenFailures, async (iRestResponse, timeSpan, retryCount, context) =>
                {
                    await Task.Run(() => Logger.LogError("The request failed. HttpStatusCode={statusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}. Uri={responseUri}; RequestResponse={responseContent}", iRestResponse.Result.StatusCode, timeSpan, retryCount, iRestResponse.Result.ResponseUri, iRestResponse.Result.Content));
                });

            var circuitBreakerPolicy = Policy
                .HandleResult<RestResponse>(x => x.StatusCode == HttpStatusCode.ServiceUnavailable)
                .CircuitBreakerAsync(1, TimeSpan.FromSeconds(60), onBreak: async (iRestResponse, timespan, context) =>
                {
                    await Task.Run(() => Logger.LogError("Circuit went into a fault state. Reason: {resultContent}", iRestResponse.Result.Content));
                },
                onReset: async (context) =>
                {
                    await Task.Run(() => Logger.LogError($"Circuit left the fault state."));
                });

            return await retryPolicy.WrapAsync(circuitBreakerPolicy).ExecuteAsync(async () => await _restClient.ExecuteAsync(restRequest));
        }

        private static string JsonSerialize(object obj) => JsonSerializer.Serialize(obj, GetJsonSerializerOptions());

        private static JsonSerializerOptions GetJsonSerializerOptions() => new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public Task<RestResponse> HttpAsync(Method httpVerb, string resource, RetryPolicy<RestResponse> retryPolicy, Dictionary<string, string>? headers = null, object? requestObject = null)
        {
            throw new NotImplementedException();
        }

        public Task<RestResponse> HttpAsync(Method httpVerb, string resource, RetryPolicy<RestResponse> retryPolicy, CircuitBreakerPolicy<RestResponse> circuitBreakerPolicy, Dictionary<string, string>? headers = null, object? requestObject = null)
        {
            throw new NotImplementedException();
        }
    }
}
