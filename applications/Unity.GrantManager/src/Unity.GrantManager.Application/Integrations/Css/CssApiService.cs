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
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Caching;
using Unity.Modules.Shared.Http;

namespace Unity.GrantManager.Integrations.Css
{
    [IntegrationService]
    [ExposeServices(typeof(CssApiService), typeof(ICssUsersApiService))]
    public class CssApiService(
        IEndpointManagementAppService endpointManagementAppService,
        IResilientHttpRequest resilientHttpRequest,
        IHttpClientFactory httpClientFactory, // Add this for token requests
        IDistributedCache<TokenValidationResponse, string> accessTokenCache,
        IOptions<CssApiOptions> cssApiOptions) : ApplicationService, ICssUsersApiService
    {
        private readonly CssApiOptions _cssApiOptions = cssApiOptions.Value;
        private const string CSS_API_KEY = "CssApiKey";

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
            try
            {
                var cssApiUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.CSS_API_BASE);
                var tokenResponse = await GetAccessTokenAsync();
                var baseUrl = $"{cssApiUrl}/{_cssApiOptions.Env}/{directory}/users";
                var url = BuildUrlWithQuery(baseUrl, parameters);

                var response = await resilientHttpRequest.HttpAsync(HttpMethod.Get, url, null, tokenResponse.AccessToken);
                
                if (response != null && response.IsSuccessStatusCode && response.Content != null)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Logger.LogWarning("Empty response received from CSS API for directory {Directory}", directory);
                        return CreateErrorResult("Empty response from CSS API");
                    }

                    try
                    {
                        var result = JsonSerializer.Deserialize<UserSearchResult>(json, _jsonOptions)!;
                        
                        if (result != null)
                        {
                            result.Success = true;
                            return result;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Logger.LogError(ex, "Failed to deserialize user search result. JSON: {Json}", json);
                        return CreateErrorResult("Failed to parse response from CSS API");
                    }
                }

                var statusCode = response?.StatusCode.ToString() ?? "Unknown";
                var errorContent = response?.Content != null ? await response.Content.ReadAsStringAsync() : "No response";
                Logger.LogWarning("CSS API request failed. Status: {StatusCode}, Content: {Content}, URL: {Url}", 
                    statusCode, errorContent, url);
                
                return CreateErrorResult($"CSS API request failed with status {statusCode}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during CSS API search for directory {Directory}", directory);
                return CreateErrorResult("Unexpected error occurred while searching users");
            }
        }

        private static UserSearchResult CreateErrorResult(string error) => new()
        {
            Success = false,
            Error = error,
            Data = []
        };

        private static string BuildUrlWithQuery(string basePath, Dictionary<string, string> queryParams)
        {
            if (queryParams.Count == 0)
                return basePath;
                
            var query = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            return $"{basePath}?{query}";
        }

        // Define this once (e.g., at the top of your class or as a static field)
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private async Task<TokenValidationResponse> GetAccessTokenAsync()
        {
            // Check cache first
            var cachedToken = await accessTokenCache.GetAsync(CSS_API_KEY);
            if (cachedToken != null && !IsTokenExpiringSoon(cachedToken))
            {
                return cachedToken;
            }

            try
            {
                var cssTokenApiUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.CSS_TOKEN_API_BASE);

                // Use HttpClientFactory instead of new HttpClient()
                using var client = httpClientFactory.CreateClient();

                var request = new HttpRequestMessage(HttpMethod.Post, cssTokenApiUrl);
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_cssApiOptions.ClientId}:{_cssApiOptions.ClientSecret}"));

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = response.Content != null ? await response.Content.ReadAsStringAsync() : "No content";
                    Logger.LogError("Failed to fetch CSS API token. Status: {StatusCode}, Content: {ErrorContent}, URL: {Url}",
                        response.StatusCode, errorContent, cssTokenApiUrl);
                    throw new UserFriendlyException($"Failed to authenticate with CSS API: {response.StatusCode}");
                }

                if (response.Content == null)
                {
                    Logger.LogError("CSS token API returned success but no content");
                    throw new UserFriendlyException("Invalid response from CSS token API");
                }

                var content = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(content))
                {
                    Logger.LogError("CSS token API returned empty content");
                    throw new UserFriendlyException("Empty response from CSS token API");
                }

                TokenValidationResponse tokenResponse;
                try
                {
                    tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(content, _jsonOptions)!;

                    if (tokenResponse == null)
                    {
                        throw new UserFriendlyException("Invalid token response format");
                    }
                }
                catch (JsonException ex)
                {
                    Logger.LogError(ex, "Failed to deserialize token response. Content: {Content}", content);
                    throw new UserFriendlyException("Failed to parse token response");
                }

                // Cache with buffer time to avoid expiration edge cases
                var cacheExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); // 60 second buffer
                await accessTokenCache.SetAsync(CSS_API_KEY, tokenResponse, new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = cacheExpiration
                });

                Logger.LogDebug("Successfully cached new CSS API token, expires at {ExpirationTime}", cacheExpiration);
                return tokenResponse;
            }
            catch (UserFriendlyException)
            {
                throw; // Re-throw user-friendly exceptions as-is
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error while fetching CSS API access token");
                throw new UserFriendlyException("Failed to authenticate with CSS API");
            }
        }

        /// <summary>
        /// Check if token is expiring within the next 5 minutes
        /// </summary>
        private static bool IsTokenExpiringSoon(TokenValidationResponse token)
        {
            if (token.ExpiresIn <= 0) return true;
            
            // Consider token expiring if it has less than 5 minutes left
            return token.ExpiresIn < 300;
        }
    }
}