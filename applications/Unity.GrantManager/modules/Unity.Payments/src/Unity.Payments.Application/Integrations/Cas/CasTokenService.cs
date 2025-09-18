using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Integrations.Css;
using Unity.Modules.Shared.Integrations;
using Unity.Payments.Integrations.Cas;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;

public class CasTokenService(
        IEndpointManagementAppService endpointManagementAppService,
        IOptions<CasClientOptions> casClientOptions,
        IHttpClientFactory httpClientFactory,
        IDistributedCache<TokenValidationResponse, string> chesTokenCache
    ) : ApplicationService, ICasTokenService
{
    private const string OAUTH_PATH = "oauth/token";
    private const string CAS_API_KEY = "CasApiKey";

    public async Task<string> GetAuthTokenAsync()
    {
        string caseBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE);
        ClientOptions clientOptions = new ClientOptions
        {
            Url = $"{caseBaseUrl}/{OAUTH_PATH}",
            ClientId = casClientOptions.Value.CasClientId,
            ClientSecret = casClientOptions.Value.CasClientSecret,
            ApiKey = CAS_API_KEY,
        };

        TokenService tokenService = new(httpClientFactory, chesTokenCache, Logger);
        return await tokenService.GetAuthTokenAsync(clientOptions);
    }
}
