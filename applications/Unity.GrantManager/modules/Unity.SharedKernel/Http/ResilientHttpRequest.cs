using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.Modules.Shared.Http
{
    [IntegrationService]
    public class ResilientHttpRequest : IResilientHttpRequest
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private static int _maxRetryAttempts = 3;
        private const int OneMinuteInSeconds = 60;       
        private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(2);
        private static TimeSpan _httpRequestTimeout = TimeSpan.FromSeconds(OneMinuteInSeconds*5); // Default to 2 minutes

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
            //specify to use TLS 1.2 as default connection if 1.3 is not available
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

            HttpResponseMessage restResponse = await _pipeline.ExecuteAsync(async ct => await httpClient.SendAsync(requestMessage, ct));
            return await Task.FromResult(restResponse);
        }
    }
}
