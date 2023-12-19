using RestSharp;
using RestSharp.Authenticators;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.GrantManager.Integration.Sso;
using Unity.GrantManager.Integrations.Http;
using Volo.Abp.Application.Services;
using System.Text.Json;
using System;
using Volo.Abp.Caching;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Linq;
using Volo.Abp;
using Unity.GrantManager.Integrations.Geocoder;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Integrations.Sso
{
    [IntegrationService]
    [ExposeServices(typeof(SsoUserApiService), typeof(ISsoUsersApiService))]
    public class SsoUserApiService : ApplicationService, ISsoUsersApiService
    {
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IDistributedCache<TokenValidationResponse, Guid> _accessTokenCache;
        private readonly IOptions<SsoApiOptions> _ssoApiOptions;
        private readonly RestClient _restClient;

        public SsoUserApiService(IResilientHttpRequest resilientHttpRequest,
            IDistributedCache<TokenValidationResponse, Guid> accessTokenCache,
            IOptions<SsoApiOptions> ssoApiOptions,
            RestClient restClient)
        {
            _resilientRestClient = resilientHttpRequest;
            _accessTokenCache = accessTokenCache;
            _ssoApiOptions = ssoApiOptions;
            _restClient = restClient;
        }

        public async Task<UserSearchResult> SearchUsersAsync(string directory, string? firstName = null, string? lastName = null)
        {
            var tokenResponse = await GetAccessTokenAsync();  
            
            var paramDictionary = new Dictionary<string, string>();
            
            if (firstName != null && firstName.Length >= 2)
            {
                paramDictionary.Add(nameof(firstName), firstName);
            }

            if (lastName != null && lastName.Length >= 2)
            {
                paramDictionary.Add(nameof(lastName), lastName);
            }

            var resource = BuildUrlWithQueryStringUsingUriBuilder($"{_ssoApiOptions.Value.ApiUrl}/{_ssoApiOptions.Value.ApiEnv}/{directory}/users?", paramDictionary);

            var authHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {tokenResponse.AccessToken}" }
            };

            var response = await _resilientRestClient.HttpAsync(Method.Get, resource, authHeaders);

            if (response != null
                && response.Content != null
                && response.IsSuccessStatusCode)
            {
                string content = response.Content;
                var result = JsonSerializer.Deserialize<UserSearchResult>(content) ?? throw new Exception();                

                result.Success = true;
                return result;
            }
            else
            {
                return new UserSearchResult() { Error = "", Success = false, Data = Array.Empty<SsoUser>() };
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
            var cachedTokenResponse = await _accessTokenCache.GetAsync(CurrentUser?.TenantId ?? Guid.Empty);

            if (cachedTokenResponse != null)
            {
                return cachedTokenResponse;
            }

            var grantType = "client_credentials";

            var request = new RestRequest($"{_ssoApiOptions.Value.TokenEndpoint}")
            {
                Authenticator = new HttpBasicAuthenticator(_ssoApiOptions.Value.Username, _ssoApiOptions.Value.Password)
            };
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type={grantType}", ParameterType.RequestBody);

            var response = await _restClient.ExecuteAsync(request, Method.Post);

            if (response.Content == null)
            {
                // handle
                throw new Exception();
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError("Error fetching Css API token {statusCode} {errorMessage} {errorException}", response.StatusCode, response.ErrorMessage, response.ErrorException);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // handle
                    throw new Exception();
                }                
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(response.Content);
            if (tokenResponse == null)
            {
                // handle
                throw new Exception();
            }

            await _accessTokenCache.SetAsync(CurrentUser?.TenantId ?? Guid.Empty, tokenResponse, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            });

            return tokenResponse;
        }
    }
}
