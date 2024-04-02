using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using System.Text.Json;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using RestSharp;
using Unity.GrantManager.Integration.Css;
using RestSharp.Authenticators;
using Unity.Notifications.Integration.Ches;
using Unity.Notifications.Integrations.Http;

namespace Unity.Notifications.Integrations.Ches
{
    [IntegrationService]
    [ExposeServices(typeof(ChesClientService), typeof(IChesClientService))]
    public class ChesClientService : ApplicationService, IChesClientService
    {
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IOptions<ChesClientOptions> _chesClientOptions;
        private readonly RestClient _restClient;

        public ChesClientService(IResilientHttpRequest resilientHttpRequest,
            IOptions<ChesClientOptions> chesClientOptions,
            RestClient restClient)
        {
            _resilientRestClient = resilientHttpRequest;
            _chesClientOptions = chesClientOptions;
            _restClient = restClient;

        }

        public async Task SendAsync(Object emailRequest)
        {
            // Ches Tokens Expire Immediately After use but we could use bulk send
            var tokenResponse = await GetAccessTokenAsync();
            var resource = $"{_chesClientOptions.Value.ChesUrl}/email";
            var authHeaders = new Dictionary<string, string>
            {
               { "Authorization", $"Bearer {tokenResponse.AccessToken}" }
            };
   
            var response = await _resilientRestClient.HttpAsync(Method.Post, resource, authHeaders, emailRequest);

            if (response != null && response.ErrorMessage != null)
            {
               throw new UserFriendlyException("ChesClientService SendAsync Exception: " + response.ErrorMessage);
            }
        }

        private async Task<TokenValidationResponse> GetAccessTokenAsync()
        {
            var grantType = "client_credentials";

            var request = new RestRequest($"{_chesClientOptions.Value.ChesTokenUrl}")
            {
                Authenticator = new HttpBasicAuthenticator(_chesClientOptions.Value.ChesClientId, _chesClientOptions.Value.ChesClientSecret)
            };

            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type={grantType}", ParameterType.RequestBody);

            var response = await _restClient.ExecuteAsync(request, Method.Post);

            if (response.Content == null)
            {
                throw new UserFriendlyException($"Error fetching Ches API token - content empty {response.StatusCode} {response.ErrorMessage}");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError("Error fetching CHES API token {statusCode} {errorMessage} {errorException}", response.StatusCode, response.ErrorMessage, response.ErrorException);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException(response.ErrorMessage);
                }
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenValidationResponse>(response.Content) ?? throw new UserFriendlyException($"Error deserializing token response {response.StatusCode} {response.ErrorMessage}");
            return tokenResponse;
        }
    }
}
