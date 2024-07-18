using Volo.Abp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Unity.Payments.Integrations.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;


namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(TokenService), typeof(ITokenService))]
    public class TokenService : ApplicationService, ITokenService
    {
        private readonly IOptions<CasClientOptions> _casClientOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;
        private const string OAUTH_PATH = "oauth/token";
        private const int ONE_MINUTE_SECONDS = 60;

        public TokenService(
            IOptions<CasClientOptions> casClientOptions,
            IHttpClientFactory httpClientFactory,
            IMemoryCache memoryCache)
        {
            _casClientOptions = casClientOptions;
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        public async Task<string> GetAuthTokenAsync()
        {
            var tokenResponse = await GetAccessTokenAsync() ?? throw new UserFriendlyException("GetAuthTokenAsync: Error retrieving Token");
            return tokenResponse.AccessToken ?? throw new UserFriendlyException("GetAuthTokenAsync: Error retrieving Access Token");
        }

        private async Task<TokenValidationResponse?> GetAccessTokenAsync()
        {
            TokenValidationResponse? tokenResponse = null;

            try
            {
                if (_memoryCache.TryGetValue("CasAuthToken", out string? authToken))
                {
                    tokenResponse = new TokenValidationResponse();
                    tokenResponse.AccessToken = authToken;
                    return tokenResponse;
                }

                string url = $"{_casClientOptions.Value.CasBaseUrl}/{OAUTH_PATH}";
                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url) { Version = new Version(3, 0) };
                List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(values);
                string authenticationString = $"{_casClientOptions.Value.CasClientId}:{_casClientOptions.Value.CasClientSecret}";
                string base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
                requestMessage.Content = content;

                //specify to use TLS 1.2 as default connection
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                HttpClient client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(url);               
                client.DefaultRequestHeaders.Accept.Clear();     
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.ConnectionClose = true;

                HttpResponseMessage response = await client.SendAsync(requestMessage);
                var responseBody = response.Content.ReadAsStringAsync();
                string responseMessage = response.RequestMessage != null ? response.RequestMessage.ToString() : "";
                if (response.Content == null)
                {
                    throw new UserFriendlyException($"Error fetching CAS API token - content empty {response.StatusCode} {response.RequestMessage}");
                }

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Logger.LogError("Error fetching CAS API token");

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException(responseMessage);
                    }
                }

                tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(responseBody.Result)
                    ?? throw new UserFriendlyException($"Error deserializing token response {response.StatusCode} {responseMessage}");
                SetupTokenCacheMemory(tokenResponse);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogInformation(ex, "Token Service Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return tokenResponse;
        }

        private void SetupTokenCacheMemory(TokenValidationResponse tokenResponse)
        {
            if (!_memoryCache.TryGetValue("CasAuthToken", out string? newAuthToken))
            {
                // Subrtact one minute from expiry for buffer
                int expiresInSeconds = tokenResponse.ExpiresIn - ONE_MINUTE_SECONDS;
                TimeSpan timeSpanExpires = TimeSpan.FromSeconds(expiresInSeconds);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(timeSpanExpires);

                _memoryCache.Set("CasAuthToken", tokenResponse.AccessToken, cacheEntryOptions);
            }
        }
    }
}