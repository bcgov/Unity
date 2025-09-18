using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Http;
using Unity.Modules.Shared.Integrations;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(CasTokenService), typeof(ICasTokenService))]
    public class CasTokenService(
        IEndpointManagementAppService endpointManagementAppService,
        IOptions<CasClientOptions> casClientOptions,
        IHttpClientFactory httpClientFactory,
        IResilientHttpRequest resilientHttpRequest,
        Microsoft.Extensions.Caching.Distributed.IDistributedCache casTokenCache,
        Microsoft.Extensions.Logging.ILogger<TokenService> tokenServiceLogger
    ) : ApplicationService, ICasTokenService
    {
        private const string OAUTH_PATH = "oauth/token";
        private const string CAS_API_KEY = "CasApiKey";

        public async Task<string> GetAuthTokenAsync(string? certificatePath = null)
        {
            string caseBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE);
            ClientOptions clientOptions = new ClientOptions
            {
                Url = $"{caseBaseUrl}/{OAUTH_PATH}",
                ClientId = casClientOptions.Value.CasClientId,
                ClientSecret = casClientOptions.Value.CasClientSecret,
                CertificatePath = certificatePath ?? string.Empty,
                ApiKey = CAS_API_KEY
            };

            TokenService tokenService = new(httpClientFactory, resilientHttpRequest, casTokenCache, tokenServiceLogger);
            var tokenResponse = await tokenService.GetAndCacheAccessTokenAsync(clientOptions);
            return tokenResponse?.AccessToken ?? string.Empty;
        }
    }
}
