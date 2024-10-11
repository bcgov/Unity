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
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;

namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(TokenService), typeof(ITokenService))]
    public class TokenService(
        IOptions<CasClientOptions> casClientOptions,
        IHttpClientFactory httpClientFactory,
        IDistributedCache<TokenValidationResponse, string> castokenCache) : ApplicationService, ITokenService
    {
        private const string OAUTH_PATH = "oauth/token";
        private const int ONE_MINUTE_SECONDS = 60;
        private const string CAS_API_KEY = "CasApiKey";

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
                // Return cached access token
                var cachedTokenResponse = await castokenCache.GetAsync(CAS_API_KEY);

                if (cachedTokenResponse != null)
                {
                    return cachedTokenResponse;
                }

                // Access token has expired or not cached yet

                return await GetAndCacheCasAccessTokenAsync();
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogInformation(ex, "Token Service Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return tokenResponse;
        }

        private async Task<TokenValidationResponse?> GetAndCacheCasAccessTokenAsync()
        {
            string url = $"{casClientOptions.Value.CasBaseUrl}/{OAUTH_PATH}";
            HttpRequestMessage requestMessage = new(HttpMethod.Post, url) { Version = new Version(3, 0) };
            List<KeyValuePair<string, string>> values =
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ];

            FormUrlEncodedContent content = new(values);
            string authenticationString = $"{casClientOptions.Value.CasClientId}:{casClientOptions.Value.CasClientSecret}";
            string base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            requestMessage.Content = content;

            //specify to use TLS 1.2 as default connection
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            HttpClient client = httpClientFactory.CreateClient();
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

            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(responseBody.Result)
                ?? throw new UserFriendlyException($"Error deserializing token response {response.StatusCode} {responseMessage}");

            int expiresInSeconds = tokenResponse.ExpiresIn - ONE_MINUTE_SECONDS;

            await castokenCache.SetAsync(CAS_API_KEY, tokenResponse, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
            });

            return tokenResponse;
        }
    }
}