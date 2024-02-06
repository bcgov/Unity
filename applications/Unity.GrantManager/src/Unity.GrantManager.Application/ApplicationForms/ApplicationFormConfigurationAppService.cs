using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Permissions;
using Unity.GrantManager.Tokens;
using Volo.Abp.Application.Services;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Encryption;

namespace Unity.GrantManager.ApplicationForms
{
    [Authorize(GrantManagerPermissions.ApplicationForms.Default)]
    public class ApplicationFormConfigurationAppService : ApplicationService, IApplicationFormConfigurationAppService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentTenant _currentTenant;
        private readonly IStringEncryptionService _stringEncryptionService;
        private readonly ITenantTokenRepository _tenantTokenRepository;

        public ApplicationFormConfigurationAppService(ICurrentTenant currentTenant,
            IHttpContextAccessor httpContextAccessor,
            IStringEncryptionService stringEncryptionService,
            ITenantTokenRepository tenantTokenRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _currentTenant = currentTenant;
            _stringEncryptionService = stringEncryptionService;
            _tenantTokenRepository = tenantTokenRepository;
        }

        public async Task<ApplicationFormsConfigurationDto> GetConfiguration()
        {
            var scheme = "https"; // default to https
            var request = _httpContextAccessor.HttpContext.Request;
            var host = request.Host.ToUriComponent();
            var pathBase = request.PathBase.ToUriComponent();
            var baseUrl = $"{scheme}://{host}{pathBase}";

            TenantToken? tenantToken = null;
            var tokenValue = string.Empty;
            if (_currentTenant.Id != null)
            {
                var qry = await _tenantTokenRepository.GetQueryableAsync();
                tenantToken = qry.FirstOrDefault(s => s.TenantId == _currentTenant.Id && s.Name == TokenConsts.IntakeApiName);
            }
            if (tenantToken != null && tenantToken.Value != null)
            {
                tokenValue = _stringEncryptionService.Decrypt(tenantToken.Value);
            }

            return new ApplicationFormsConfigurationDto()
            {
                EventSubscriptionConfiguration = new EventSubscriptionConfigurationDto()
                {
                    Key = AuthConstants.ApiKeyHeader,
                    EndpointToken = tokenValue ?? string.Empty,
                    EndpointUrl = $"{baseUrl}/api/chefs/event/{_currentTenant.Id}",
                }
            };
        }
    }
}
