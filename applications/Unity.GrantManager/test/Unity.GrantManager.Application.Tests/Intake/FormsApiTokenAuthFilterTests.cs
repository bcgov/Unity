using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Controllers.Auth.FormSubmission;
using Unity.GrantManager.Controllers.Authentication.FormSubmission;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Routing;
using Unity.GrantManager.Events;
using Unity.GrantManager.Controllers;
using System;
using Shouldly;
using Unity.GrantManager.Tokens;
using Volo.Abp.Security.Encryption;
using Unity.GrantManager.Controllers.Authentication.FormSubmission.FormIdResolvers;
using Newtonsoft.Json.Serialization;

namespace Unity.GrantManager.Intake
{
    public class FormsApiTokenAuthFilterTests : GrantManagerApplicationTestBase
    {
        private readonly FormsApiTokenAuthFilter _formsApiTokenAuthFilter;

        private readonly ITenantRepository _tenantRepository;        
        private readonly ICurrentTenant _currentTenant;
        private readonly IApplicationFormTokenAppService _formTokenAppService;
        private readonly IEnumerable<IFormIdResolver> _formIdResolvers;
        private readonly ITenantTokenRepository _tenantTokenRepository;
        private readonly IStringEncryptionService _stringEncryptionService;

        public FormsApiTokenAuthFilterTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _tenantRepository = GetRequiredService<ITenantRepository>();
            _currentTenant = GetRequiredService<ICurrentTenant>();
            _formTokenAppService = GetRequiredService<IApplicationFormTokenAppService>();
            _formIdResolvers = new List<IFormIdResolver>() { new FormIdRequestBodyResolver() };
            _tenantTokenRepository = GetRequiredService<ITenantTokenRepository>();
            _stringEncryptionService = GetRequiredService<StringEncryptionService>();

            _formsApiTokenAuthFilter = new FormsApiTokenAuthFilter(_tenantRepository, _currentTenant, _formTokenAppService, _formIdResolvers);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task OnAuthorizationAsync_AllowLoginWithNoTokenSet()
        {                     
            // Arrange
            var context = BuildContext(new EventSubscriptionDto()
            {
                FormId = Guid.NewGuid(),
                FormVersion = Guid.NewGuid(),
                SubmissionId = Guid.NewGuid(),
                SubscriptionEvent = "string"
            });

            // Act
            using (_currentTenant.Change(Guid.NewGuid(), "Test"))
            {
                // Create an AuthorizationFilterContext
                await _formsApiTokenAuthFilter.OnAuthorizationAsync(context);
            }

            // Assert            
            context.Result.ShouldBe(null);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task OnAuthorizationAsync_401_MissingApiHeader()
        {     
            var tenantId = Guid.NewGuid();
            var tenantName = "Test";

            // Arrange
            var context = BuildContext(new EventSubscriptionDto()
            {
                FormId = Guid.NewGuid(),
                FormVersion = Guid.NewGuid(),
                SubmissionId = Guid.NewGuid(),
                SubscriptionEvent = "string"
            });

            await _tenantTokenRepository.InsertAsync(new TenantToken() 
            { 
                TenantId = tenantId, 
                Name = TokenConsts.IntakeApiName,
                Value = _stringEncryptionService.Encrypt(_formTokenAppService.GenerateFormApiToken())
            }, true);

            // Act
            using (_currentTenant.Change(tenantId, tenantName))
            {                
                // Create an AuthorizationFilterContext
                await _formsApiTokenAuthFilter.OnAuthorizationAsync(context);
            }

            // Assert            
            context.Result.ShouldBeOfType(typeof(UnauthorizedObjectResult));
            var result = context.Result as UnauthorizedObjectResult;
            result!.Value.ShouldBe("API Key missing");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task OnAuthorizationAsync_401_NoFormIdApiHeader()
        {
            var tenantId = Guid.NewGuid();
            var tenantName = "Test";

            // Arrange
            var context = BuildContext(new EventSubscriptionDto()
            {
                FormVersion = Guid.NewGuid(),
                SubmissionId = Guid.NewGuid(),
                SubscriptionEvent = "string"
            }, new List<KeyValuePair<string, string>> 
            { new KeyValuePair<string, string>(AuthConstants.ApiKeyHeader, "TOKEN") });

            await _tenantTokenRepository.InsertAsync(new TenantToken()
            {
                TenantId = tenantId,
                Name = TokenConsts.IntakeApiName,
                Value = _stringEncryptionService.Encrypt(_formTokenAppService.GenerateFormApiToken())
            }, true);

            // Act
            using (_currentTenant.Change(tenantId, tenantName))
            {
                // Create an AuthorizationFilterContext
                await _formsApiTokenAuthFilter.OnAuthorizationAsync(context);
            }

            // Assert            
            context.Result.ShouldBeOfType(typeof(UnauthorizedObjectResult));
            var result = context.Result as UnauthorizedObjectResult;
            result!.Value.ShouldBe("Invalid Form Id");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task OnAuthorizationAsync_401_InvalidToken()
        {
            var tenantId = Guid.NewGuid();
            var tenantName = "Test";

            // Arrange
            var context = BuildContext(new EventSubscriptionDto()
            {
                FormId = Guid.NewGuid(),
                FormVersion = Guid.NewGuid(),
                SubmissionId = Guid.NewGuid(),
                SubscriptionEvent = "string"
            }, new List<KeyValuePair<string, string>>
            { new KeyValuePair<string, string>(AuthConstants.ApiKeyHeader, "TOKEN") });

            await _tenantTokenRepository.InsertAsync(new TenantToken()
            {
                TenantId = tenantId,
                Name = TokenConsts.IntakeApiName,
                Value = _stringEncryptionService.Encrypt(_formTokenAppService.GenerateFormApiToken())
            }, true);

            // Act
            using (_currentTenant.Change(tenantId, tenantName))
            {
                // Create an AuthorizationFilterContext
                await _formsApiTokenAuthFilter.OnAuthorizationAsync(context);
            }

            // Assert            
            context.Result.ShouldBeOfType(typeof(UnauthorizedObjectResult));
            var result = context.Result as UnauthorizedObjectResult;
            result!.Value.ShouldBe("Invalid API Key");
        }

        private AuthorizationFilterContext BuildContext(EventSubscriptionDto eventSubscription, 
            List<KeyValuePair<string, string>>? headers = null)
        {
            var httpContext = new DefaultHttpContext();

            var json = JsonConvert.SerializeObject(eventSubscription, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            httpContext.Request.Body = stream;
            httpContext.Request.ContentLength = stream.Length;
            httpContext.Request.ContentType = "application/json";

            if (headers != null)
            {
                foreach (var kvp in headers)
                {
                    httpContext.Request.Headers.Append(kvp.Key, kvp.Value);
                }
            }

            var actionDescriptor = new ControllerActionDescriptor
            {
                ActionName = nameof(EventSubscriptionController.PostEventSubscriptionAsync)
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }
    }
}
