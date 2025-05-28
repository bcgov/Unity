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
        private static TimeSpan _httpRequestTimeout = TimeSpan.FromSeconds(OneMinuteInSeconds);
        private string? _baseUrl;

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

        public void SetBaseUrl(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Base URL is not a valid absolute URI.", nameof(baseUrl));
            }

            _baseUrl = baseUrl.TrimEnd('/');
        }

        private static bool ShouldRetry(HttpStatusCode statusCode)
        {
            HttpStatusCode[] retryableStatusCodes = {
                HttpStatusCode.TooManyRequests,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout,
            };

            return retryableStatusCodes.Contains(statusCode);
        }

        private static ResiliencePipeline<HttpResponseMessage> _pipeline =
            new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(result => ShouldRetry(result.StatusCode)),
                    Delay = _pauseBetweenFailures,
                    MaxRetryAttempts = _maxRetryAttempts,
                    UseJitter = true,
                    BackoffType = DelayBackoffType.Exponential,
                    OnRetry = args => default
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

        public async Task<HttpResponseMessage> ExecuteRequestAsync(
            HttpMethod httpVerb,
            string resource,
            string? body,
            string? authToken,
            (string username, string password)? basicAuth = null)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                throw new InvalidOperationException("Base URL has not been set. Call SetBaseUrl() before making requests.");
            }

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var baseUri = new Uri(_baseUrl, UriKind.Absolute);
            var relativeUri = new Uri(resource, UriKind.Relative);
            var fullUrl = new Uri(baseUri, relativeUri);

            var requestMessage = new HttpRequestMessage(httpVerb, fullUrl)
            {
                Version = new Version(3, 0)
            };

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.ConnectionClose = true;

            if (!authToken.IsNullOrWhiteSpace())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            }

            if (httpVerb != HttpMethod.Get && body != null)
            {
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response = await _pipeline.ExecuteAsync(async ct =>
                await httpClient.SendAsync(requestMessage, ct));

            return response;
        }
    }
}
