using Volo.Abp;
using RestSharp;
using RestSharp.Authenticators;
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


namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(TokenService), typeof(ITokenService))]
    public class TokenService : ApplicationService, ITokenService
    {   private readonly IOptions<CasClientOptions> _casClientOptions;
        private readonly RestClient _restClient;
        private const string OAUTH_PATH = "oauth/token";

        public TokenService(
            IOptions<CasClientOptions> casClientOptions,
            RestClient restClient)
        {
            _casClientOptions = casClientOptions;
            _restClient = restClient;
        }

        public async Task<Dictionary<string, string>> GetAuthHeadersAsync() {
            var tokenResponse = await GetAccessTokenAsync();

            Dictionary<string, string> authHeaders = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {tokenResponse.AccessToken}" }
            };
            return authHeaders;
        }

 		private async Task<TokenValidationResponse> GetAccessTokenAsync()
        {
            var grantType = "client_credentials";

            var request = new RestRequest($"{_casClientOptions.Value.CasBaseUrl}/{OAUTH_PATH}")
            {
                Authenticator = new HttpBasicAuthenticator(_casClientOptions.Value.CasClientId, _casClientOptions.Value.CasClientSecret)
            };

            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", $"grant_type={grantType}", ParameterType.RequestBody);

            var response = await _restClient.ExecuteAsync(request, Method.Post);

            if (response.Content == null)
            {
                throw new UserFriendlyException($"Error fetching CAS API token - content empty {response.StatusCode} {response.ErrorMessage}");
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                string StatusCode = response.StatusCode;
                string ErrorMessage = response.ErrorMessage;
                string ErrorException = response.ErrorException;
                Logger.LogError("Error fetching CAS API token {StatusCode} {ErrorMessage} {ErrorException}", StatusCode, ErrorMessage, ErrorException);

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