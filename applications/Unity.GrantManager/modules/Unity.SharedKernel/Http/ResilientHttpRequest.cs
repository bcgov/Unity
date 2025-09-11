using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Microsoft.Extensions.Logging;

namespace Unity.Modules.Shared.Http
{
    [IntegrationService]
    public class ResilientHttpRequest : IResilientHttpRequest
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ResilientHttpRequest>? _logger;
        private string? _baseUrl;
        private bool _disposed;

        // Standard configuration
        private static int _maxRetryAttempts = 3;
        private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(2);
        private static TimeSpan _httpRequestTimeout = TimeSpan.FromSeconds(60);

        // Long-lived connection configuration
        private static int _longLivedMaxRetryAttempts = 2; // Fewer retries for long requests
        private static TimeSpan _longLivedPauseBetweenFailures = TimeSpan.FromSeconds(5);
        private static TimeSpan _longLivedHttpRequestTimeout = TimeSpan.FromMinutes(3);

        private const string AuthorizationHeader = "Authorization";
        private const string ContentTypeJson = "application/json";

        private const string MustBeNonNegativeMessage = "Must be non-negative";
        private const string MustBePositiveMessage = "Must be positive";

        private static volatile ResiliencePipeline<HttpResponseMessage>? _pipeline;
        private static volatile ResiliencePipeline<HttpResponseMessage>? _longLivedPipeline;
        private static readonly object _pipelineLock = new object();

        private static readonly HttpStatusCode[] RetryableStatusCodes =
        [
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.RequestTimeout
        ];

        public ResilientHttpRequest(HttpClient httpClient, ILogger<ResilientHttpRequest>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger;
        }




        /// <summary>
        /// Configure the standard pipeline options (default: 60s timeout, 3 retries).
        /// </summary>
        public static void SetPipelineOptions(
            int maxRetryAttempts,
            TimeSpan pauseBetweenFailures,
            TimeSpan httpRequestTimeout)
        {
            if (maxRetryAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), MustBeNonNegativeMessage);
            if (pauseBetweenFailures < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(pauseBetweenFailures), MustBeNonNegativeMessage);
            if (httpRequestTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(httpRequestTimeout), MustBePositiveMessage);

            lock (_pipelineLock)
            {
                _maxRetryAttempts = maxRetryAttempts;
                _pauseBetweenFailures = pauseBetweenFailures;
                _httpRequestTimeout = httpRequestTimeout;
                _pipeline = null; // Force rebuild on next access
            }
        }

        /// <summary>
        /// Configure the long-lived pipeline options (default: 3min timeout, 2 retries).
        /// </summary>
        public static void SetLongLivedPipelineOptions(
            int maxRetryAttempts,
            TimeSpan pauseBetweenFailures,
            TimeSpan httpRequestTimeout)
        {
            if (maxRetryAttempts < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetryAttempts), "Must be non-negative");
            if (pauseBetweenFailures < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(pauseBetweenFailures), "Must be non-negative");
            if (httpRequestTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(httpRequestTimeout), "Must be positive");

            lock (_pipelineLock)
            {
                _longLivedMaxRetryAttempts = maxRetryAttempts;
                _longLivedPauseBetweenFailures = pauseBetweenFailures;
                _longLivedHttpRequestTimeout = httpRequestTimeout;
                _longLivedPipeline = null; // Force rebuild on next access
            }
        }

        private static ResiliencePipeline<HttpResponseMessage> GetOrCreatePipeline()
        {
            if (_pipeline != null)
                return _pipeline;

            lock (_pipelineLock)
            {
                return _pipeline ??= BuildPipeline(_maxRetryAttempts, _pauseBetweenFailures, _httpRequestTimeout);
            }
        }

        private static ResiliencePipeline<HttpResponseMessage> GetOrCreateLongLivedPipeline()
        {
            if (_longLivedPipeline != null)
                return _longLivedPipeline;

            lock (_pipelineLock)
            {
                return _longLivedPipeline ??= BuildPipeline(_longLivedMaxRetryAttempts, _longLivedPauseBetweenFailures, _longLivedHttpRequestTimeout);
            }
        }

        private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(
            int maxRetryAttempts, 
            TimeSpan pauseBetweenFailures, 
            TimeSpan httpRequestTimeout)
        {
            var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

            if (maxRetryAttempts > 0)
            {
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(result => ShouldRetry(result.StatusCode)),
                    Delay = pauseBetweenFailures,
                    MaxRetryAttempts = maxRetryAttempts,
                    UseJitter = true,
                    BackoffType = DelayBackoffType.Exponential,
                    OnRetry = args =>
                    {
                        // Retry attempt occurred for HTTP request
                        return ValueTask.CompletedTask;
                    }
                });
            }

            return builder
                .AddTimeout(httpRequestTimeout)
                .Build();
        }

        public void SetBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be null or whitespace.", nameof(baseUrl));

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException("Base URL is not a valid absolute URI.", nameof(baseUrl));

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Base URL must use HTTP or HTTPS scheme.", nameof(baseUrl));

            _baseUrl = baseUrl.TrimEnd('/');
        }

        private static bool ShouldRetry(HttpStatusCode statusCode) =>
            RetryableStatusCodes.Contains(statusCode);

        /// <summary>
        /// Send an HTTP request with standard resilience policies applied.
        /// Default: 60-second timeout, 3 retries, 2-second pause between retries.
        /// </summary>
        public async Task<HttpResponseMessage> HttpAsync(
            HttpMethod httpVerb,
            string resource,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteHttpRequestAsync(
                httpVerb, 
                resource, 
                body, 
                authToken, 
                basicAuth, 
                isLongLived: false, 
                cancellationToken);
        }

        /// <summary>
        /// Send a long-lived HTTP request with extended timeout and resilience policies applied.
        /// Default: 3-minute timeout, 2 retries, 5-second pause between retries.
        /// Suitable for operations that may take extended time such as large data processing or report generation.
        /// </summary>
        public async Task<HttpResponseMessage> HttpLongLivedAsync(
            HttpMethod httpVerb,
            string resource,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteHttpRequestAsync(
                httpVerb, 
                resource, 
                body, 
                authToken, 
                basicAuth, 
                isLongLived: true, 
                cancellationToken);
        }

        private async Task<HttpResponseMessage> ExecuteHttpRequestAsync(
            HttpMethod httpVerb,
            string resource,
            object? body,
            string? authToken,
            (string username, string password)? basicAuth,
            bool isLongLived,
            CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(ResilientHttpRequest));
            ArgumentNullException.ThrowIfNull(httpVerb);
            if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be null or whitespace.", nameof(resource));

            var fullUrl = BuildFullUrl(resource);
            var sanitizedFullUrl = fullUrl.ToString().Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
            var pipeline = isLongLived ? GetOrCreateLongLivedPipeline() : GetOrCreatePipeline();
            var requestType = isLongLived ? "long-lived" : "standard";
            var timeoutDuration = isLongLived ? _longLivedHttpRequestTimeout : _httpRequestTimeout;

            try
            {
            _logger?.LogDebug("Starting {RequestType} HTTP {Method} request to {Url} (timeout: {Timeout})", 
                requestType, httpVerb, sanitizedFullUrl, timeoutDuration);

            return await pipeline.ExecuteAsync(async ct =>
            {
                using var requestMessage = CreateHttpRequestMessage(httpVerb, fullUrl, body, authToken, basicAuth);
                
                var response = await _httpClient.SendAsync(requestMessage, ct);
                
                _logger?.LogDebug("Received HTTP {StatusCode} response from {RequestType} {Method} {Url}", 
                response.StatusCode, requestType, httpVerb, sanitizedFullUrl);
                
                return response;
            }, cancellationToken);
            }
            catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException || cancellationToken.IsCancellationRequested)
            {
            _logger?.LogWarning(ex, "{RequestType} HTTP request timed out after {Timeout} for {Method} {Url}. Exception: {Exception}", 
                requestType, timeoutDuration, httpVerb, sanitizedFullUrl, ex);
            throw new TimeoutException(
                $"The {requestType} HTTP request timed out after {timeoutDuration} for {httpVerb} {sanitizedFullUrl}.", ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
            _logger?.LogError(ex, "{RequestType} HTTP request failed for {Method} {Url}", 
                requestType, httpVerb, sanitizedFullUrl);
            throw new HttpRequestException(
                $"The {requestType} HTTP request failed for {httpVerb} {sanitizedFullUrl}. See inner exception for details.", ex);
            }
        }

        private Uri BuildFullUrl(string resource)
        {
            if (Uri.TryCreate(resource, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                throw new InvalidOperationException(
                    "Base URL must be set when using relative resource paths.");
            }

            if (!Uri.TryCreate(new Uri(_baseUrl, UriKind.Absolute), resource, out var combinedUri))
            {
                throw new ArgumentException($"Cannot combine base URL '{_baseUrl}' with resource '{resource}'.");
            }

            return combinedUri;
        }

        private static HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod httpVerb,
            Uri fullUrl,
            object? body,
            string? authToken,
            (string username, string password)? basicAuth)
        {
            var requestMessage = new HttpRequestMessage(httpVerb, fullUrl);

            // Set headers
            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.ConnectionClose = true;

            // Authentication
            SetAuthenticationHeader(requestMessage, authToken, basicAuth);

            // Body content
            if (body != null)
            {
                requestMessage.Content = CreateJsonContent(body);
            }

            return requestMessage;
        }

        private static void SetAuthenticationHeader(
            HttpRequestMessage request,
            string? authToken,
            (string username, string password)? basicAuth)
        {
            request.Headers.Remove(AuthorizationHeader);

            if (!string.IsNullOrWhiteSpace(authToken))
            {
                request.Headers.Add(AuthorizationHeader, $"Bearer {authToken}");
            }
            else if (basicAuth.HasValue)
            {
                var (username, password) = basicAuth.Value;
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException("Username and password cannot be null or whitespace for basic authentication.");
                }

                var credentials = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{username}:{password}"));
                request.Headers.Add(AuthorizationHeader, $"Basic {credentials}");
            }
        }

        private static readonly JsonSerializerOptions CamelCaseOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private static StringContent CreateJsonContent(object body)
        {
            string bodyString = body switch
            {
            string s => s,
            _ => JsonSerializer.Serialize(body, CamelCaseOptions)
            };

            return new StringContent(bodyString, Encoding.UTF8, ContentTypeJson);
        }

        /// <summary>
        /// Utility method to read HTTP content as string.
        /// </summary>
        public static async Task<string> ContentToStringAsync(HttpContent httpContent)
        {
            ArgumentNullException.ThrowIfNull(httpContent);
            return await httpContent.ReadAsStringAsync();
        }

        /// <summary>
        /// Utility method to read and deserialize HTTP content as JSON.
        /// </summary>
        public static async Task<T?> ContentToJsonAsync<T>(HttpContent httpContent, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(httpContent);

            var content = await httpContent.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            return default;

            options ??= new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<T>(content, options);
        }

        /// <summary>
        /// Get current configuration for standard pipeline.
        /// </summary>
        public static (int MaxRetries, TimeSpan PauseBetweenFailures, TimeSpan Timeout) GetStandardConfiguration()
        {
            return (_maxRetryAttempts, _pauseBetweenFailures, _httpRequestTimeout);
        }

        /// <summary>
        /// Get current configuration for long-lived pipeline.
        /// </summary>
        public static (int MaxRetries, TimeSpan PauseBetweenFailures, TimeSpan Timeout) GetLongLivedConfiguration()
        {
            return (_longLivedMaxRetryAttempts, _longLivedPauseBetweenFailures, _longLivedHttpRequestTimeout);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here if needed
                    // (Do NOT dispose _httpClient if managed by DI)
                }
                // Dispose unmanaged resources here if any

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}