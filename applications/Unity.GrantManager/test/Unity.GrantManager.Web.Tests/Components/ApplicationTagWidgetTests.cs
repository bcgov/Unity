using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicationTagsWidget;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class ApplicationTagsWidgetTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task ApplicationTagsWidgetReturnsStatus()
        {
            // Arrange
            var applicationService = Substitute.For<IApplicationTagsService>();
            var expected = "Mock";
            var applicationId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            applicationService.GetApplicationTagsAsync(applicationId).Returns(await Task.FromResult(new ApplicationTagsDto() { ApplicationId = Guid.Empty, Text = "Mock" }));

            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ApplicationTagsWidgetViewComponent(applicationService)
            {
                ViewComponentContext = viewComponentContext
            };

            //Act
            var result = await viewComponent.InvokeAsync(applicationId) as ViewViewComponentResult;
            ApplicationTagsWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ApplicationTagsWidgetViewModel;

            //Assert
            resultModel!.ApplicationTags.ShouldBe(expected);
        }
    }
}
