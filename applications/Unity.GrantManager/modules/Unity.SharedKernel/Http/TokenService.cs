using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Unity.Modules.Shared.Integrations;

namespace Unity.Modules.Shared.Http
{
    public class TokenService(
        IHttpClientFactory httpClientFactory,
        IResilientHttpRequest resilientHttpRequest,
        IDistributedCache chesTokenCache,
        ILogger<TokenService> logger)
    {
        private const int ONE_MINUTE_SECONDS = 60;

        public async Task<TokenValidationResponse?> GetAndCacheAccessTokenAsync(ClientOptions clientOptions)
        {
            ArgumentNullException.ThrowIfNull(clientOptions);
            HttpResponseMessage response;

            if (!string.IsNullOrWhiteSpace(clientOptions.CertificatePath))
            {
                // Use ResilientHttpRequest with mutual TLS
                var body = new Dictionary<string, string> { { "grant_type", "client_credentials" } };
                string authenticationString = $"{clientOptions.ClientId}:{clientOptions.ClientSecret}";
                string base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

                response = await resilientHttpRequest.HttpAsyncSecured(
                    HttpMethod.Post,
                    clientOptions.Url,
                    clientOptions.CertificatePath,
                    clientOptions.CertificatePassword,
                    body: body,
                    authToken: $"Basic {base64EncodedAuthenticationString}"
                );
            }
            else
            {
                // Standard HttpClient request
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, clientOptions.Url)
                {
                    Version = new Version(3, 0)
                };

                var values = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "client_credentials")
                };

                requestMessage.Content = new FormUrlEncodedContent(values);

                string authenticationString = $"{clientOptions.ClientId}:{clientOptions.ClientSecret}";
                string base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

                HttpClient client = httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(clientOptions.Url);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.ConnectionClose = true;

                response = await client.SendAsync(requestMessage);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            string responseMessage = response.RequestMessage?.ToString() ?? "";

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

            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(responseBody)
                ?? throw new UserFriendlyException($"Error deserializing token response {response.StatusCode} {responseMessage}");

            int expiresInSeconds = tokenResponse.ExpiresIn - ONE_MINUTE_SECONDS;

            var tokenResponseBytes = JsonSerializer.SerializeToUtf8Bytes(tokenResponse);
            await chesTokenCache.SetAsync(clientOptions.ApiKey, tokenResponseBytes, new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
            });

            return tokenResponse;
        }
    }

}
