using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Unity.Modules.Integrations;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Unity.Payments.Integrations.Cas
{
    [IntegrationService]
    [ExposeServices(typeof(CasTokenService), typeof(ICasTokenService))]
    public class CasTokenService(
        IOptions<CasClientOptions> casClientOptions,
        IHttpClientFactory httpClientFactory,
        IDistributedCache<TokenValidationResponse, string> chesTokenCache
    ) : ApplicationService, ICasTokenService
    {
        private const string OAUTH_PATH = "oauth/token";
        private const string CAS_API_KEY = "CasApiKey";

        public async Task<string> GetAuthTokenAsync()
        {
            ClientOptions clientOptions = new ClientOptions
            {
                Url = $"{casClientOptions.Value.CasBaseUrl}/{OAUTH_PATH}",
                ClientId = casClientOptions.Value.CasClientId,
                ClientSecret = casClientOptions.Value.CasClientSecret,
                ApiKey = CAS_API_KEY,
            };

            TokenService tokenService = new TokenService(httpClientFactory, chesTokenCache);
            return await tokenService.GetAuthTokenAsync(clientOptions);
        }
    }
}
