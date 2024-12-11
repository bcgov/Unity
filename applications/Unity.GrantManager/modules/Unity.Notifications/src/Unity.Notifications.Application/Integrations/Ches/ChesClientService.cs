using Volo.Abp;
using System.Threading.Tasks;
using System.Text.Json;
using Unity.Modules.Integrations;
using Unity.Modules.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using System.Net.Http;
using Volo.Abp.Caching;

namespace Unity.Notifications.Integrations.Ches
{
    [IntegrationService]
    [RemoteService(false, Name = "Ches")]
    [ExposeServices(typeof(ChesClientService), typeof(IChesClientService))]
    public class ChesClientService : ApplicationService, IChesClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IOptions<ChesClientOptions> _chesClientOptions;
        private readonly IDistributedCache<TokenValidationResponse, string> _chesTokenCache;

        public ChesClientService(
            IDistributedCache<TokenValidationResponse, string> chesTokenCache,
            IResilientHttpRequest resilientHttpRequest,
            IHttpClientFactory httpClientFactory,
            IOptions<ChesClientOptions> chesClientOptions)
        {
            _resilientRestClient = resilientHttpRequest;
            _chesClientOptions = chesClientOptions;
            _httpClientFactory = httpClientFactory;
            _chesTokenCache = chesTokenCache;
        }

        public async Task<HttpResponseMessage?> SendAsync(object emailRequest)
        {
            ClientOptions clientOptions = new ClientOptions
            {
                Url = _chesClientOptions.Value.ChesTokenUrl,
                ClientId = _chesClientOptions.Value.ChesClientId,
                ClientSecret = _chesClientOptions.Value.ChesClientSecret,
                ApiKey = "ChesApiKey"
            };

            TokenService tokenService = new TokenService(_httpClientFactory, _chesTokenCache);
            var authToken = await tokenService.GetAuthTokenAsync(clientOptions);
            var resource = $"{_chesClientOptions.Value.ChesUrl}/email";
            string jsonString = JsonSerializer.Serialize(emailRequest);
            var response = await _resilientRestClient.HttpAsyncWithBody(HttpMethod.Post, resource, jsonString, authToken);
            return response;
        }
    }
}
