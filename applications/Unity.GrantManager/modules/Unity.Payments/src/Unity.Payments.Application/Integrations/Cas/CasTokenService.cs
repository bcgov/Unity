using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Unity.GrantManager.Integrations;
using Unity.Modules.Shared.Integrations;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;
using Unity.GrantManager.Integrations.Css;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;
namespace Unity.Payments.Integrations.Cas
{
    [RemoteService(false)]
    [IntegrationService]
    [ExposeServices(typeof(CasTokenService), typeof(ICasTokenService))]
    public class CasTokenService(
        IConfiguration configuration,
        ICasClientCodeLookupService casClientCodeLookupService,
        IEndpointManagementAppService endpointManagementAppService,
        ITenantRepository tenantRepository,
        IHttpClientFactory httpClientFactory,
        IDistributedCache<TokenValidationResponse, string> chesTokenCache
    ) : ApplicationService, ICasTokenService
    {
        private const string OAUTH_PATH = "oauth/token";
        private const string CAS_API_KEY = "CasApiKey";

        [AllowAnonymous]
        public async Task<string> GetAuthTokenAsync(Guid tenantId)
        {
            var caseBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE);

            var tenant = await tenantRepository.GetAsync(tenantId);
            var casClientCode = tenant.ExtraProperties?["CasClientCode"]?.ToString();

            if (string.IsNullOrEmpty(casClientCode))
            {
                throw new UserFriendlyException("No CAS client code configured for the current tenant. Please contact your administrator.");
            }

            var casClientId = await casClientCodeLookupService.GetClientIdByCasClientCodeAsync(casClientCode)
                ?? throw new UserFriendlyException($"No CAS client configuration found for CAS client code: {casClientCode}");

            var clientSecret = configuration.GetValue<string>($"CAS_API_KEY_{casClientCode.ToUpper()}") ?? string.Empty;

            return await new TokenService(httpClientFactory, chesTokenCache, Logger).GetAuthTokenAsync(new ClientOptions
            {
                Url = $"{caseBaseUrl}/{OAUTH_PATH}",
                ClientId = casClientId,
                ClientSecret = clientSecret,
                ApiKey = CAS_API_KEY,
            });            
        }
    }
}
