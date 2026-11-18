using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Unity.Notifications.Emails;
using Xunit;

namespace Unity.GrantManager.Components
{
    // These are real HTTP/host-level checks (via WebTestFixture's WebApplicationFactory<Program>),
    // unlike AttachmentControllerTests, which construct AttachmentController directly with mocks and
    // so never exercise ASP.NET Core's routing/authorization/conventional-controller pipeline. Only a
    // test at this level can actually prove [Authorize] is in effect.
    [Collection(WebTestCollection.Name)]
    public class AttachmentSecurityTests
    {
        private readonly WebTestFixture _fixture;

        public AttachmentSecurityTests(WebTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task AttachmentController_Download_AnonymousRequest_IsNotAllowed()
        {
            // Arrange - a plain client with no auth cookie/token, i.e. a direct API caller rather
            // than a browser that went through an authenticated page first.
            var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // Act
            var response = await client.GetAsync($"/api/app/attachment/applicant/{Guid.NewGuid()}/download/test.pdf");

            // Assert - must not succeed anonymously. Depending on how the auth challenge resolves
            // for an API-shaped request this is either a 401 or a redirect (3xx) to login - either
            // way it must not be a 200.
            response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
        }

        [Fact]
        public void EmailLogAttachmentAppService_UploadAsync_IsNotExposedAsHttpEndpoint()
        {
            using var scope = _fixture.Services.CreateScope();
            var actionDescriptorProvider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

            var uploadIsExposed = actionDescriptorProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Any(a => a.MethodInfo.DeclaringType == typeof(EmailLogAttachmentAppService)
                    && a.MethodInfo.Name == nameof(EmailLogAttachmentAppService.UploadAsync));

            uploadIsExposed.ShouldBeFalse("Raw attachment uploads must only be reached in-process after controller validation.");
        }

        [Fact]
        public void EmailLogAttachmentAppService_DeleteAsync_IsStillExposedAsHttpEndpoint()
        {
            // Sanity check that conventional-controller generation for this class as a whole
            // still works as expected for a normal, intentionally-exposed action.
            using var scope = _fixture.Services.CreateScope();
            var actionDescriptorProvider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

            var deleteIsExposed = actionDescriptorProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Any(a => a.MethodInfo.DeclaringType == typeof(EmailLogAttachmentAppService)
                    && a.MethodInfo.Name == nameof(EmailLogAttachmentAppService.DeleteAsync));

            deleteIsExposed.ShouldBeTrue("Expected DeleteAsync to remain exposed as an HTTP endpoint.");
        }
    }
}
