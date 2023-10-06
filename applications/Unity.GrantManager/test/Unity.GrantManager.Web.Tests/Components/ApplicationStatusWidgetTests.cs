using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicationStatusWidget;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class ApplicationStatusWidgetTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task ApplicationStatusWidgetReturnsStatus()
        {
            // Arrange
            var applicationService = Substitute.For<IGrantApplicationAppService>();
            var expected = "Mock";
            var applicationId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            applicationService.GetApplicationStatusAsync(applicationId).Returns(await Task.FromResult(new ApplicationStatusDto() { InternalStatus = "Mock", ExternalStatus = "MockExt" }));

            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ApplicationStatusWidgetViewComponent(applicationService)
            {
                ViewComponentContext = viewComponentContext
            };

            //Act
            var result = await viewComponent.InvokeAsync(applicationId) as ViewViewComponentResult;
            ApplicationStatusWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ApplicationStatusWidgetViewModel;

            //Assert
            resultModel!.ApplicationStatus.ShouldBe(expected);
        }
    }
}
