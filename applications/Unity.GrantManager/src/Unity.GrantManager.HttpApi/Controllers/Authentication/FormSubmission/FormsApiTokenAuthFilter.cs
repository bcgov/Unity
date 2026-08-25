using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Unity.GrantManager.ApplicationForms;
using System;
using System.Collections.Generic;
using Unity.GrantManager.Controllers.Authentication.FormSubmission;

namespace Unity.GrantManager.Controllers.Auth.FormSubmission
{
    public class FormsApiTokenAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IApplicationFormTokenAppService _formTokenAppService;
        private readonly IEnumerable<IFormIdResolver> _formIdResolvers;
        private readonly IHostEnvironment _hostEnvironment;

        public FormsApiTokenAuthFilter(ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            IApplicationFormTokenAppService formTokenAppService,
            IEnumerable<IFormIdResolver> formIdResolvers,
            IHostEnvironment hostEnvironment)
        {
            _currentTenant = currentTenant;
            _tenantRepository = tenantRepository;
            _formTokenAppService = formTokenAppService;
            _formIdResolvers = formIdResolvers;
            _hostEnvironment = hostEnvironment;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var apiToken = await GetTenantApiTokenAsync();
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                if (_hostEnvironment.IsDevelopment())
                {
                    return; // Dev-only convenience: unconfigured tenants pass through locally
                }

                context.Result = new UnauthorizedObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "API authentication not configured for this tenant",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
                });
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeader, out var extractedApiToken))
            {
                context.Result = new UnauthorizedObjectResult("API Key missing");
                return;
            }

            Guid? formId = await ResolveFormIdAsync(context);

            if (formId == null)
            {
                context.Result = new UnauthorizedObjectResult("Invalid Form Id");
                return;
            }

            if (apiToken != extractedApiToken)
            {
                context.Result = new UnauthorizedObjectResult("Invalid API Key");
            }
        }

        private async Task<string?> GetTenantApiTokenAsync()
        {
            string? apiToken;

            if (_currentTenant.Id == null)
            {
                var defaultTenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);
                using (_currentTenant.Change(defaultTenant.Id, defaultTenant.Name))
                {
                    apiToken = await _formTokenAppService.GetFormApiTokenAsync();
                }
            }
            else
            {
                apiToken = await _formTokenAppService.GetFormApiTokenAsync();
            }

            return apiToken;
        }

        private async Task<Guid?> ResolveFormIdAsync(AuthorizationFilterContext context)
        {
            foreach (var formIdResolver in _formIdResolvers)
            {
                var formId = await formIdResolver.ResolvedFormIdAsync(context);

                if (formId != null && formId != Guid.Empty)
                    return formId;
            }

            return null;
        }
    }
}
