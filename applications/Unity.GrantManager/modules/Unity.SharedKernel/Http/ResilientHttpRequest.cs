using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Newtonsoft.Json;
using System.Security.Authentication;

namespace Unity.Modules.Shared.Http
{
    [IntegrationService]
    public class ResilientHttpRequest(HttpClient httpClient) : IResilientHttpRequest
    {
        private static int _maxRetryAttempts = 3;
        private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(2);
        private static TimeSpan _httpRequestTimeout = TimeSpan.FromSeconds(60);

        private const string AuthorizationHeader = "Authorization";

        private string? _baseUrl;
        private readonly HttpClient _httpClient = httpClient;

        // Keep a cached mutual TLS HttpClient — never create per request
        private static readonly object _mtlsClientLock = new();
        private static HttpClient? _mtlsClient;

        /// <summary>
        /// Status codes that qualify for retry.
        /// </summary>
        private static readonly HttpStatusCode[] RetryableStatusCodes =
        [
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        ];

        /// <summary>
        /// A Polly v8 pipeline for handling retries + timeout.
        /// </summary>
        private static ResiliencePipeline<HttpResponseMessage> _pipeline = BuildPipeline();


        // -------------------------------
        // Pipeline Configuration
        // -------------------------------

        public static void SetPipelineOptions(int maxRetryAttempts, TimeSpan pauseBetweenFailures, TimeSpan httpRequestTimeout)
        {
            _maxRetryAttempts = maxRetryAttempts;
            _pauseBetweenFailures = pauseBetweenFailures;
            _httpRequestTimeout = httpRequestTimeout;

            _pipeline = BuildPipeline();
        }

        private static ResiliencePipeline<HttpResponseMessage> BuildPipeline()
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()           // most HTTP/network errors
                        .Handle<IOException>()                    // transport layer failures
                        .Handle<SocketException>()                // TCP reset / handshake abort
                        .Handle<AuthenticationException>()        // TLS handshake authentication failures
                        .Handle<OperationCanceledException>()     // handshake timeout
                        .HandleResult(result => ShouldRetry(result.StatusCode)),

                    MaxRetryAttempts = _maxRetryAttempts,
                    Delay = _pauseBetweenFailures,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                })
                .AddTimeout(_httpRequestTimeout)
                .Build();
        }

        private static bool ShouldRetry(HttpStatusCode statusCode) =>
            RetryableStatusCodes.Contains(statusCode);


        // -------------------------------
        // URL handling
        // -------------------------------

        public void SetBaseUrl(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Base URL is not a valid absolute URI.", nameof(baseUrl));
            }
            _baseUrl = baseUrl.TrimEnd('/');
        }


        // -------------------------------
        // HTTP Request Entry Points
        // -------------------------------

        public async Task<HttpResponseMessage> HttpAsync(
            HttpMethod httpVerb,
            string resource,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default)
        {
            return await SendWithClientAsync(
                _httpClient, httpVerb, resource, body, authToken, basicAuth, cancellationToken);
        }


        /// <summary>
        /// HTTPS + Client Certificate (mTLS)
        /// This version now *reuses* the mTLS HttpClient safely.
        /// </summary>
        public Task<HttpResponseMessage> HttpAsyncSecured(
            HttpMethod httpVerb,
            string resource,
            string certPath,
            string? certPassword = null,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default)
        {
            EnsureMutualTlsClient(certPath, certPassword);

            return SendWithClientAsync(
                _mtlsClient!, httpVerb, resource, body, authToken, basicAuth, cancellationToken);
        }


        // -------------------------------
        // Mutual TLS Client Factory
        // -------------------------------

        private static void EnsureMutualTlsClient(string certPath, string? certPassword)
        {
            if (_mtlsClient != null)
                return;

            lock (_mtlsClientLock)
            {
                if (_mtlsClient != null)
                    return;

                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    // Prevent handshake failures due to slow TLS negotiation
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                };

                var cert = LoadCertificate(certPath, certPassword);
                handler.ClientCertificates.Add(cert);

                _mtlsClient = new HttpClient(handler);
            }
        }


        private static X509Certificate2 LoadCertificate(string certPath, string? certPassword)
        {
            if (string.IsNullOrWhiteSpace(certPassword))
            {
                return X509CertificateLoader.LoadCertificateFromFile(certPath);
            }
            return X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
        }


        // -------------------------------
        // Core Send Logic
        // -------------------------------

        private async Task<HttpResponseMessage> SendWithClientAsync(
            HttpClient client,
            HttpMethod httpVerb,
            string resource,
            object? body,
            string? authToken,
            (string username, string password)? basicAuth,
            CancellationToken cancellationToken)
        {
            // Build final URL
            if (!Uri.TryCreate(resource, UriKind.Absolute, out Uri? fullUrl))
            {
                if (_baseUrl == null)
                {
                    throw new InvalidOperationException("Base URL must be set for relative paths.");
                }
                fullUrl = new Uri(new Uri(_baseUrl), resource);
            }

            return await _pipeline.ExecuteAsync(async ct =>
            {
                using var requestMessage =
                    BuildRequestMessage(httpVerb, fullUrl, body, authToken, basicAuth);

                return await client.SendAsync(requestMessage, ct)
                                   .ConfigureAwait(false);

            }, cancellationToken);
        }


        // -------------------------------
        // Build HTTP Request Message
        // -------------------------------

        private static HttpRequestMessage BuildRequestMessage(
            HttpMethod httpVerb,
            Uri fullUrl,
            object? body,
            string? authToken,
            (string username, string password)? basicAuth)
        {
            var requestMessage = new HttpRequestMessage(httpVerb, fullUrl);
            requestMessage.Headers.Accept.Clear();

            // NO Connection: close — this caused constant TLS renegotiation
            // requestMessage.Headers.ConnectionClose = true;

            // Bearer Token
            if (!string.IsNullOrWhiteSpace(authToken))
            {
                requestMessage.Headers.Remove(AuthorizationHeader);
                requestMessage.Headers.Add(AuthorizationHeader, $"Bearer {authToken}");
            }

            // Basic Auth
            else if (basicAuth.HasValue)
            {
                string raw = $"{basicAuth.Value.username}:{basicAuth.Value.password}";
                string encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(raw));

                requestMessage.Headers.Remove(AuthorizationHeader);
                requestMessage.Headers.Add(AuthorizationHeader, $"Basic {encoded}");
            }

            // Body
            if (body != null)
            {
                string payload = body is string s ? s : JsonConvert.SerializeObject(body);
                requestMessage.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            }

            return requestMessage;
        }


        // -------------------------------
        // Misc Helpers
        // -------------------------------

        public static Task<string> ContentToStringAsync(HttpContent httpContent)
            => httpContent.ReadAsStringAsync();
    }
}
