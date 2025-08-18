using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Volo.Abp;

namespace Unity.Modules.Shared.Http
{
    [IntegrationService]
    public class ResilientHttpRequest(HttpClient httpClient) : IResilientHttpRequest
    {
        private static int _maxRetryAttempts = 3;
        private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(2);
        private static TimeSpan _httpRequestTimeout = TimeSpan.FromSeconds(60);

        private const string AuthorizationHeader = "Authorization";

        private static ResiliencePipeline<HttpResponseMessage> _pipeline = BuildPipeline();

        private string? _baseUrl;
        private readonly HttpClient _httpClient = httpClient;

        public static void SetPipelineOptions(
            int maxRetryAttempts,
            TimeSpan pauseBetweenFailures,
            TimeSpan httpRequestTimeout)
        {
            _maxRetryAttempts = maxRetryAttempts;
            _pauseBetweenFailures = pauseBetweenFailures;
            _httpRequestTimeout = httpRequestTimeout;

            _pipeline = BuildPipeline(); // rebuild with new settings
        }

        private static ResiliencePipeline<HttpResponseMessage> BuildPipeline()
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(result => ShouldRetry(result.StatusCode)),
                    Delay = _pauseBetweenFailures,
                    MaxRetryAttempts = _maxRetryAttempts,
                    UseJitter = true,
                    BackoffType = DelayBackoffType.Exponential
                })
                .AddTimeout(_httpRequestTimeout)
                .Build();
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
            var retryableStatusCodes = new[]
            {
                HttpStatusCode.TooManyRequests,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.BadGateway,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            };

            return retryableStatusCodes.Contains(statusCode);
        }

        public async Task<HttpResponseMessage> HttpAsync(HttpMethod httpVerb, string resource, string? authToken = null)
        {
            return await ExecuteRequestAsync(httpVerb, resource, null, authToken);
        }

        public async Task<HttpResponseMessage> HttpAsyncWithBody(HttpMethod httpVerb, string resource, string? body = null, string? authToken = null)
        {
            return await ExecuteRequestAsync(httpVerb, resource, body, authToken);
        }

        public static async Task<string> ContentToStringAsync(HttpContent httpContent)
        {
            return await httpContent.ReadAsStringAsync();
        }

        public async Task<HttpResponseMessage> ExecuteRequestAsync(
           HttpMethod httpVerb,
           string resource,
           string? body,
           string? authToken,
           (string username, string password)? basicAuth = null)
        {
            // Determine final URL
            if (!Uri.TryCreate(resource, UriKind.Absolute, out Uri? fullUrl))
            {
                if (string.IsNullOrWhiteSpace(_baseUrl))
                {
                    throw new InvalidOperationException("Base URL must be set for relative paths.");
                }
                fullUrl = new Uri(new Uri(_baseUrl, UriKind.Absolute), resource);
            }

            // Execute through resilience pipeline
            return await _pipeline.ExecuteAsync(async ct =>
            {
                using var requestMessage = new HttpRequestMessage(httpVerb, fullUrl)
                {
                    Version = new Version(3, 0)
                };

                // Headers are per-request, not global
                requestMessage.Headers.Accept.Clear();
                requestMessage.Headers.ConnectionClose = true;

                if (!string.IsNullOrWhiteSpace(authToken))
                {
                    requestMessage.Headers.Remove(AuthorizationHeader);
                    requestMessage.Headers.Add(AuthorizationHeader, $"Bearer {authToken}");
                }
                else if (basicAuth.HasValue)
                {
                    var credentials = Convert.ToBase64String(
                        System.Text.Encoding.ASCII.GetBytes($"{basicAuth.Value.username}:{basicAuth.Value.password}")
                    );
                    requestMessage.Headers.Remove(AuthorizationHeader);
                    requestMessage.Headers.Add(AuthorizationHeader, $"Basic {credentials}");
                }

                if (!string.IsNullOrWhiteSpace(body))
                {
                    requestMessage.Content = new StringContent(body);
                }

                return await _httpClient.SendAsync(requestMessage, ct);
            });
        }
    }
}
