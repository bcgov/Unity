using Volo.Abp;
using System.Threading.Tasks;
using Unity.Modules.Shared.Integrations;
using Unity.Modules.Shared.Http;
using Volo.Abp.Application.Services;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using System.Net.Http;
using Volo.Abp.Caching;
using Unity.GrantManager.Integrations;

namespace Unity.Notifications.Integrations.Ches
{
    [IntegrationService]
    [RemoteService(false, Name = "Ches")]
    [ExposeServices(typeof(ChesClientService), typeof(IChesClientService))]
    public class ChesClientService(
        IDistributedCache<TokenValidationResponse, string> chesTokenCache,
        IResilientHttpRequest resilientHttpRequest,
        IEndpointManagementAppService endpointManagementAppService,
        IHttpClientFactory httpClientFactory,
        IOptions<ChesClientOptions> chesClientOptions
    ) : ApplicationService, IChesClientService
    {
        public async Task<HttpResponseMessage?> SendAsync(object emailRequest)
        {
            string authToken = await GetAuthTokenAsync();
            string notificationsApiUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.NOTIFICATION_API_BASE);
            var resource = $"{notificationsApiUrl}/email";

            // Pass the object directly; ResilientHttpRequest will serialize it to JSON
            var response = await resilientHttpRequest.HttpAsync(
                HttpMethod.Post,
                resource,
                emailRequest,
                authToken
            );

            return response;
        }

        private async Task<string> GetAuthTokenAsync()
        {
            string notificationsAuthUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.NOTIFICATION_AUTH);

            ClientOptions clientOptions = new()
            {
                Url = notificationsAuthUrl,
                ClientId = chesClientOptions.Value.ChesClientId,
                ClientSecret = chesClientOptions.Value.ChesClientSecret,
                ApiKey = "ChesApiKey"
            };

            TokenService tokenService = new(httpClientFactory, chesTokenCache, Logger);
            return await tokenService.GetAuthTokenAsync(clientOptions);
        }
    }
}
