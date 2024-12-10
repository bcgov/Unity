using Volo.Abp;
using System.Threading.Tasks;
using System.Text.Json;
using Unity.Notifications.Integrations.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using System.Net.Http;

namespace Unity.Notifications.Integrations.Ches
{
    [IntegrationService]
    [ExposeServices(typeof(ChesClientService), typeof(IChesClientService))]
    public class ChesClientService : ApplicationService, IChesClientService
    {
        private readonly ITokenService _iTokenService;
        private readonly IResilientHttpRequest _resilientRestClient;
        private readonly IOptions<ChesClientOptions> _chesClientOptions;

        public ChesClientService(
            ITokenService iTokenService,
            IResilientHttpRequest resilientHttpRequest,
            IOptions<ChesClientOptions> chesClientOptions)
        {
            _iTokenService = iTokenService;
            _resilientRestClient = resilientHttpRequest;
            _chesClientOptions = chesClientOptions;
        }

        public async Task<HttpResponseMessage?> SendAsync(object emailRequest)
        {
            // Ches Tokens Expire Immediately After use but we could use bulk send
            var authToken = await _iTokenService.GetAuthTokenAsync();
            var resource = $"{_chesClientOptions.Value.ChesUrl}/email";
            string jsonString = JsonSerializer.Serialize(emailRequest);
            var response = await _resilientRestClient.HttpAsyncWithBody(HttpMethod.Post, resource, jsonString, authToken);
            return response;
        }
    }
}