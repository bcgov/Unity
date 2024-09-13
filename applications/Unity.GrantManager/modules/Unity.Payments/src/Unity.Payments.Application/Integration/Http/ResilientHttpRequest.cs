using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.Payments.Integrations.Http
{
    [IntegrationService]
    public class ResilientHttpRequest : PaymentsAppService, IResilientHttpRequest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static int _maxRetryAttempts = 3;
        private static int OneMinuteInSeconds = 60; // CAS API takes 20 seconds
        private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(2);
        private static TimeSpan _httpRequestTimeout = TimeSpan.FromSeconds(OneMinuteInSeconds);

        public ResilientHttpRequest(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public static void SetPipelineOptions(
            int maxRetryAttempts, 
            TimeSpan pauseBetweenFailures,
            TimeSpan httpRequestTimeout
            )
        {
            _maxRetryAttempts = maxRetryAttempts;
            _pauseBetweenFailures = pauseBetweenFailures;
            _httpRequestTimeout = httpRequestTimeout;
        }

        private static bool ReprocessBasedOnStatusCode(HttpStatusCode statusCode)
        {
            HttpStatusCode[] reprocessStatusCodes = {
                 HttpStatusCode.TooManyRequests,
                 HttpStatusCode.InternalServerError,
                 HttpStatusCode.BadGateway,
                 HttpStatusCode.ServiceUnavailable,
                 HttpStatusCode.GatewayTimeout,
            };

            return reprocessStatusCodes.Contains(statusCode);
        }

        private static ResiliencePipeline<HttpResponseMessage> _pipeline =
                new ResiliencePipelineBuilder<HttpResponseMessage>()
                   .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                   {
                       ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                           .Handle<HttpRequestException>()
                           .HandleResult(result => ReprocessBasedOnStatusCode(result.StatusCode)),
                       Delay = _pauseBetweenFailures,
                       MaxRetryAttempts = _maxRetryAttempts,
                       UseJitter = true,
                       BackoffType = DelayBackoffType.Exponential,
                       OnRetry = args =>
                       {
                           return default;
                       }
                   })
                   .AddTimeout(_httpRequestTimeout)
                   .Build();

        public async Task<HttpResponseMessage> HttpAsync(HttpMethod httpVerb, string resource, string? authToken = null)
        {
            return await ExecuteRequestAsync(httpVerb, resource, null, authToken);
        }

        public async Task<HttpResponseMessage> HttpAsyncWithBody(HttpMethod httpVerb, string resource, string? body = null, string? authToken = null)
        {
            return await ExecuteRequestAsync(httpVerb, resource, body, authToken);
        }

        public static string ContentToString(HttpContent httpContent)
        {
            var readAsStringAsync = httpContent.ReadAsStringAsync();
            return readAsStringAsync.Result;
        }

        private async Task<HttpResponseMessage> ExecuteRequestAsync(
            HttpMethod httpVerb, 
            string resource,
            string? body,
            string? authToken)
        {
            HttpResponseMessage restResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable
            };

            try
            {
                //specify to use TLS 1.2 as default connection
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                HttpRequestMessage requestMessage = new HttpRequestMessage(httpVerb, resource) { Version = new Version(3, 0) };
                using HttpClient httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.ConnectionClose = true;

                if (!authToken.IsNullOrEmpty())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
                }

                if (httpVerb != HttpMethod.Get && body != null)
                {
                    requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                restResponse = await _pipeline.ExecuteAsync(async ct => await httpClient.SendAsync(requestMessage, ct));
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogInformation(ex, "An Exception was thrown from ExecuteRequestAsync: {ExceptionMessage}", ExceptionMessage);
            }

            return await Task.FromResult(restResponse);
        }

    }
}
