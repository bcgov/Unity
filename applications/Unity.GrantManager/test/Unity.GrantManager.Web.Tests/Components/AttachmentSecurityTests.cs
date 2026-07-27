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
    // test at this level can actually prove [Authorize] and [RemoteService(false)] are in effect.
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
            // EmailLogAttachmentAppService.UploadAsync has none of the allowlist/size/content-type
            // checks AttachmentController enforces before calling it, so it must never be reachable
            // as an auto-generated HTTP action - only in-process, via IEmailLogAttachmentUploadService.
            using var scope = _fixture.Services.CreateScope();
            var actionDescriptorProvider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();

            var uploadIsExposed = actionDescriptorProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Any(a => a.MethodInfo.DeclaringType == typeof(EmailLogAttachmentAppService)
                    && a.MethodInfo.Name == nameof(EmailLogAttachmentAppService.UploadAsync));

            uploadIsExposed.ShouldBeFalse(
                "EmailLogAttachmentAppService.UploadAsync must stay [RemoteService(false)] - it has no upload validation of its own.");
        }

        [Fact]
        public void EmailLogAttachmentAppService_DeleteAsync_IsStillExposedAsHttpEndpoint()
        {
            // Sanity check that conventional-controller generation for this class as a whole still
            // works, and the RemoteService(false) fix on UploadAsync didn't suppress everything.
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
