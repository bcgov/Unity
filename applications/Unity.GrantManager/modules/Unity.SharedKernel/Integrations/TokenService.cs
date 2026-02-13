using Volo.Abp;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Unity.GrantManager.Integrations.Css;

namespace Unity.Modules.Shared.Integrations
{
    public class TokenService(
        IHttpClientFactory httpClientFactory,
        IDistributedCache<TokenValidationResponse, string> chesTokenCache,
        ILogger logger)
    {
        private const int ONE_MINUTE_SECONDS = 60;

        public async Task<string> GetAuthTokenAsync(ClientOptions clientOptions)
        {
            logger.LogError("Getting auth token for {Url} with client id {ClientId}", clientOptions.Url, clientOptions.ClientId);
            logger.LogError("Getting auth token for {Url} with client id {ClientSecret}", clientOptions.Url, clientOptions.ClientSecret);
            var tokenResponse = await GetAccessTokenAsync(clientOptions) ?? throw new UserFriendlyException("GetAuthTokenAsync: Error retrieving Token");
            return tokenResponse.AccessToken ?? throw new UserFriendlyException("GetAuthTokenAsync: Error retrieving Access Token");
        }

        private async Task<TokenValidationResponse?> GetAccessTokenAsync(ClientOptions clientOptions)
        {
            TokenValidationResponse? tokenResponse = null;

            try
            {
                // Return cached access token
                var cachedTokenResponse = await chesTokenCache.GetAsync(clientOptions.ApiKey);

                if (cachedTokenResponse != null)
                {
                    return cachedTokenResponse;
                }

                // Access token has expired or not cached yet
                return await GetAndCacheAccessTokenAsync(clientOptions);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                logger.LogInformation(ex, "Token Service Exception: {ExceptionMessage}", ExceptionMessage);
            }

            return tokenResponse;
        }

        private async Task<TokenValidationResponse?> GetAndCacheAccessTokenAsync(ClientOptions clientOptions)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Post, clientOptions.Url) { Version = new Version(3, 0) };
            List<KeyValuePair<string, string>> values =
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ];

            FormUrlEncodedContent content = new(values);
            string authenticationString = $"{clientOptions.ClientId}:{clientOptions.ClientSecret}";
            string base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(authenticationString));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            requestMessage.Content = content;

            HttpClient client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(clientOptions.Url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            HttpResponseMessage response = await client.SendAsync(requestMessage);
            var responseBody = response.Content.ReadAsStringAsync();
            string responseMessage = response.RequestMessage != null ? response.RequestMessage.ToString() : "";

            if (response.Content == null)
            {
                throw new UserFriendlyException($"Error fetching CHES API token - content empty {response.StatusCode} {response.RequestMessage}");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Error fetching CHES API token");

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException(responseMessage);
                }
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(responseBody.Result)
                ?? throw new UserFriendlyException($"Error deserializing token response {response.StatusCode} {responseMessage}");

            int expiresInSeconds = tokenResponse.ExpiresIn - ONE_MINUTE_SECONDS;

            await chesTokenCache.SetAsync(clientOptions.ApiKey, tokenResponse, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
            });

            return tokenResponse;
        }
    }
}