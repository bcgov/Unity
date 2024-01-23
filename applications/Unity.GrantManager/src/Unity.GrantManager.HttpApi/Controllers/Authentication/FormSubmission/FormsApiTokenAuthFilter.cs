using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Unity.GrantManager.Controllers.Authentication;
using Unity.GrantManager.ApplicationForms;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Unity.GrantManager.Controllers.Auth.FormSubmission
{
    public class FormsApiTokenAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IApplicationFormApiAppService _formsApiAppService;
        private readonly IEnumerable<IFormIdResolver> _formIdResolvers;

        public FormsApiTokenAuthFilter(ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            IApplicationFormApiAppService formsApiAppService,
            IEnumerable<IFormIdResolver> formIdResolvers)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _formsApiAppService = formsApiAppService;
            _formIdResolvers = formIdResolvers;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeader, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key missing");
                return;
            }

            var formId = await ResolveFormIdAsync(context);

            if (formId == null)
            {
                context.Result = new UnauthorizedObjectResult("Invalid Form Id");
                return;
            }

            var apiKey = await GetFormApiKeyAsync(formId.Value);
            if (!apiKey.Equals(extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("Invalid API Key");
            }
        }

        private async Task<string> GetFormApiKeyAsync(Guid formId)
        {
            if (_currentTenant.Id == null)
            {
                var defaultTenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.DefaultTenantName);
                using (_currentTenant.Change(defaultTenant.Id, defaultTenant.Name))
                {
                    return await _formsApiAppService.GetToken(formId);
                }
            }

            return _currentTenant.Id.Value.ToString();
        }

        private async Task<Guid?> ResolveFormIdAsync(AuthorizationFilterContext context)
        {
            foreach (var formIdResolver in _formIdResolvers)
            {
                var formId = await formIdResolver.ResolvedFormIdAsync(context);
                
                if (formId != null)
                    return formId;
            }

            return null;
        }
    }
}
