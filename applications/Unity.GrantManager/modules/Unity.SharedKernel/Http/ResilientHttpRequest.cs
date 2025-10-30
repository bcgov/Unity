using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp;
using Newtonsoft.Json;

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

        private static readonly HttpStatusCode[] RetryableStatusCodes = new[]
        {
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
        };

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

        private static bool ShouldRetry(HttpStatusCode statusCode) => RetryableStatusCodes.Contains(statusCode);

        /// <summary>
        /// Send an HTTP request with resilience policies applied.
        /// </summary>
        public async Task<HttpResponseMessage> HttpAsync(
            HttpMethod httpVerb,
            string resource,
            object? body = null,
            string? authToken = null,
            (string username, string password)? basicAuth = null,
            CancellationToken cancellationToken = default)
        {
            return await SendWithClientAsync(_httpClient, httpVerb, resource, body, authToken, basicAuth, cancellationToken);
        }

        /// <summary>
        /// HTTP request with mutual TLS (client certificate).
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
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual
            };

            X509Certificate2 clientCert = LoadCertificate(certPath, certPassword);
            handler.ClientCertificates.Add(clientCert);

            using var securedHttpClient = new HttpClient(handler);
            return SendWithClientAsync(securedHttpClient, httpVerb, resource, body, authToken, basicAuth, cancellationToken);
        }

        private static X509Certificate2 LoadCertificate(string certPath, string? certPassword = null)
        {
            if (string.IsNullOrWhiteSpace(certPassword))
            {
                // Load PEM or DER certificates
                return X509CertificateLoader.LoadCertificateFromFile(certPath);
            }
            else
            {
                // Load PFX/PKCS12 certificates with password
                return X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
            }
        }

        private async Task<HttpResponseMessage> SendWithClientAsync(
            HttpClient client,
            HttpMethod httpVerb,
            string resource,
            object? body,
            string? authToken,
            (string username, string password)? basicAuth,
            CancellationToken cancellationToken)
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

            return await _pipeline.ExecuteAsync(async ct =>
            {
                using var requestMessage = BuildRequestMessage(httpVerb, fullUrl, body, authToken, basicAuth);
                return await client.SendAsync(requestMessage, ct);
            }, cancellationToken);
        }

        private static HttpRequestMessage BuildRequestMessage(
            HttpMethod httpVerb,
            Uri fullUrl,
            object? body,
            string? authToken,
            (string username, string password)? basicAuth)
        {
            var requestMessage = new HttpRequestMessage(httpVerb, fullUrl);
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
                    Encoding.ASCII.GetBytes($"{basicAuth.Value.username}:{basicAuth.Value.password}")
                );
                requestMessage.Headers.Remove(AuthorizationHeader);
                requestMessage.Headers.Add(AuthorizationHeader, $"Basic {credentials}");
            }

            if (body != null)
            {
                string bodyString = body is string s ? s : JsonConvert.SerializeObject(body);
                requestMessage.Content = new StringContent(
                    bodyString, Encoding.UTF8, "application/json"
                );
            }

            return requestMessage;
        }

        public static async Task<string> ContentToStringAsync(HttpContent httpContent)
        {
            return await httpContent.ReadAsStringAsync();
        }
    }
}
