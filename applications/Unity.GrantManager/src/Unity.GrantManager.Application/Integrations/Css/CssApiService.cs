using RestSharp;
using RestSharp.Authenticators;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.GrantManager.Integrations.Http;
using Volo.Abp.Application.Services;
using System.Text.Json;
using System;
using Volo.Abp.Caching;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Linq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Integration.Css;

namespace Unity.GrantManager.Integrations.Sso
{
    [IntegrationService]
    [ExposeServices(typeof(CssApiService), typeof(ICssUsersApiService))]
    public class CssApiService(IResilientHttpRequest resilientHttpRequest,
        IDistributedCache<TokenValidationResponse, string> accessTokenCache,
        IOptions<CssApiOptions> cssApiOptions,
        RestClient restClient) : ApplicationService, ICssUsersApiService
    {
        private const string CSS_API_KEY = "CssApiKey";

        public async Task<UserSearchResult> FindUserAsync(string directory, string guid)
        {
            var paramDictionary = new Dictionary<string, string>();

            if (guid != null)
            {
                paramDictionary.Add(nameof(guid), guid);
            }

            return await SearchSsoAsync(directory, paramDictionary);
        }

        public async Task<UserSearchResult> SearchUsersAsync(string directory, string? firstName = null, string? lastName = null)
        {            
            var paramDictionary = new Dictionary<string, string>();

            if (firstName != null && firstName.Length >= 2)
            {
                paramDictionary.Add(nameof(firstName), firstName);
            }

            if (lastName != null && lastName.Length >= 2)
            {
                paramDictionary.Add(nameof(lastName), lastName);
            }

            return await SearchSsoAsync(directory, paramDictionary);
        }

        private async Task<UserSearchResult> SearchSsoAsync(string directory, Dictionary<string, string> paramDictionary)
        {
            var tokenResponse = await GetAccessTokenAsync();

            var resource = BuildUrlWithQueryStringUsingUriBuilder($"{cssApiOptions.Value.Url}/{cssApiOptions.Value.Env}/{directory}/users?", paramDictionary);

            var authHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {tokenResponse.AccessToken}" }
            };

            var response = await resilientHttpRequest.HttpAsync(Method.Get, resource, authHeaders);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                var result = JsonSerializer.Deserialize<UserSearchResult>(content) ?? throw new UserFriendlyException("SearchSsoAsync -> Could not Deserialize");

                result.Success = true;
                return result;
            }
            else
            {
                return new UserSearchResult() { Error = "", Success = false, Data = Array.Empty<CssUser>() };
            }
        }

        private static string BuildUrlWithQueryStringUsingUriBuilder(string basePath, Dictionary<string, string> queryParams)
        {
            var uriBuilder = new UriBuilder(basePath)
            {
                Query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"))
            };
            return uriBuilder.Uri.AbsoluteUri;
        }

        private async Task<TokenValidationResponse> GetAccessTokenAsync()
        {
            var cachedTokenResponse = await accessTokenCache.GetAsync(CSS_API_KEY);

            if (cachedTokenResponse != null)
            {
                return cachedTokenResponse;
            }

            var grantType = "client_credentials";

            var request = new RestRequest($"{cssApiOptions.Value.TokenUrl}")
            {
                Authenticator = new HttpBasicAuthenticator(cssApiOptions.Value.ClientId, cssApiOptions.Value.ClientSecret)
            };
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type={grantType}", ParameterType.RequestBody);

            var response = await restClient.ExecuteAsync(request, Method.Post);

            if (response.Content == null)
            {
                throw new UserFriendlyException($"Error fetching Css API token - content empty {response.StatusCode} {response.ErrorMessage}");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError(response.ErrorException, "Error fetching Css API token {StatusCode} {ErrorMessage} {ErrorException}", response.StatusCode, response.ErrorMessage, response.ErrorException);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {                    
                    throw new UnauthorizedAccessException(response.ErrorMessage);
                }
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(response.Content) ?? throw new UserFriendlyException($"Error deserializing token response {response.StatusCode} {response.ErrorMessage}");
            await accessTokenCache.SetAsync(CSS_API_KEY, tokenResponse, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            });

            return tokenResponse;
        }
    }
}
