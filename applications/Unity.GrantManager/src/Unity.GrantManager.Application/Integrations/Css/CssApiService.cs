using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.GrantManager.Integrations.Css;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Caching;
using Unity.Modules.Shared.Http;

namespace Unity.GrantManager.Integrations.Sso
{
    [IntegrationService]
    [ExposeServices(typeof(CssApiService), typeof(ICssUsersApiService))]
    public class CssApiService : ApplicationService, ICssUsersApiService
    {
        private readonly IResilientHttpRequest _resilientHttpRequest;
        private readonly IDistributedCache<TokenValidationResponse, string> _accessTokenCache;
        private readonly CssApiOptions _cssApiOptions;
        private const string CSS_API_KEY = "CssApiKey";

        public CssApiService(
            IResilientHttpRequest resilientHttpRequest,
            IDistributedCache<TokenValidationResponse, string> accessTokenCache,
            IOptions<CssApiOptions> cssApiOptions)
        {
            _resilientHttpRequest = resilientHttpRequest;
            _accessTokenCache = accessTokenCache;
            _cssApiOptions = cssApiOptions.Value;
        }

        public async Task<UserSearchResult> FindUserAsync(string directory, string guid)
        {
            var parameters = new Dictionary<string, string> { { nameof(guid), guid } };
            return await SearchSsoAsync(directory, parameters);
        }

        public async Task<UserSearchResult> SearchUsersAsync(string directory, string? firstName = null, string? lastName = null)
        {
            var parameters = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(firstName) && firstName.Length >= 2)
                parameters.Add(nameof(firstName), firstName);

            if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length >= 2)
                parameters.Add(nameof(lastName), lastName);

            return await SearchSsoAsync(directory, parameters);
        }

        private async Task<UserSearchResult> SearchSsoAsync(string directory, Dictionary<string, string> parameters)
        {
            var tokenResponse = await GetAccessTokenAsync();
            var baseUrl = $"{_cssApiOptions.Url}/{_cssApiOptions.Env}/{directory}/users";
            var url = BuildUrlWithQuery(baseUrl, parameters);

            _resilientHttpRequest.SetBaseUrl("");
            var response = await _resilientHttpRequest.ExecuteRequestAsync(HttpMethod.Get, url, null, tokenResponse.AccessToken);

            if (response != null && response.IsSuccessStatusCode && response.Content != null)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<UserSearchResult>(json) ?? throw new UserFriendlyException("Could not deserialize user search result.");
                result.Success = true;
                return result;
            }

            return new UserSearchResult
            {
                Success = false,
                Error = "Failed to search users",
                Data = Array.Empty<CssUser>()
            };
        }

        private static string BuildUrlWithQuery(string basePath, Dictionary<string, string> queryParams)
        {
            var query = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            return string.IsNullOrWhiteSpace(query) ? basePath : $"{basePath}?{query}";
        }

        private async Task<TokenValidationResponse> GetAccessTokenAsync()
        {
            var cachedToken = await _accessTokenCache.GetAsync(CSS_API_KEY);
            if (cachedToken != null)
                return cachedToken;

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _cssApiOptions.TokenUrl);

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_cssApiOptions.ClientId}:{_cssApiOptions.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                var errorContent = response.Content != null ? await response.Content.ReadAsStringAsync() : "No content";
                Logger.LogError("Failed to fetch CSS API token. Status: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                throw new UserFriendlyException($"Error fetching token: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(content) ?? throw new UserFriendlyException("Could not parse token response.");

            await _accessTokenCache.SetAsync(CSS_API_KEY, tokenResponse, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            });

            return tokenResponse;
        }
    }
}
