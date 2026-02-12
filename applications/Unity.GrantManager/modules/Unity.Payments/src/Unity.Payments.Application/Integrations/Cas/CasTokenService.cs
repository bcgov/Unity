using System;
using System.Collections.Generic;
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
using Unity.TenantManagement;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
namespace Unity.Payments.Integrations.Cas
{
    [AllowAnonymous]
    [IntegrationService]
    [ExposeServices(typeof(CasTokenService), typeof(ICasTokenService))]

    public class CasTokenService(
        IConfiguration configuration,
        ICasClientCodeLookupService casClientCodeLookupService,
        IEndpointManagementAppService endpointManagementAppService,
        ITenantAppService tenantAppService,
        IHttpClientFactory httpClientFactory,
        IDistributedCache<TokenValidationResponse, string> chesTokenCache
    ) : ApplicationService, ICasTokenService
    {
        private const string OAUTH_PATH = "oauth/token";
        private const string CAS_API_KEY = "CasApiKey";

        public async Task<string> GetAuthTokenAsync(Guid tenantId)
        {
                if (tenantId == Guid.Empty)
                {
                    throw new UserFriendlyException("Tenant ID cannot be empty.");
                }

                var caseBaseUrl = await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.PAYMENT_API_BASE);
                var casClientCode = await tenantAppService.GetCurrentTenantCasClientClientCode(tenantId);

                if (string.IsNullOrEmpty(casClientCode))
                {
                    throw new UserFriendlyException("No CAS client code configured for the current tenant. Please contact your administrator.");
                }

                var casClientId = await casClientCodeLookupService.GetClientIdByCasClientCodeAsync(casClientCode)
                    ?? throw new UserFriendlyException($"No CAS client configuration found for CAS client code: {casClientCode}");

                var casApiKeys = configuration.GetSection("CAS_API_KEYS").Get<Dictionary<string, string>>() ?? [];
                var clientSecret = casApiKeys.TryGetValue($"CAS_API_KEY_{casClientCode}".ToUpper(), out var value)
                    ? value
                    : throw new UserFriendlyException($"No CAS API key configured for CAS client code: {casClientCode}.");

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
