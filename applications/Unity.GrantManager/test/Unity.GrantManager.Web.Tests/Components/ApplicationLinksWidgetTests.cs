using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicationLinksWidget;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class ApplicationLinksWidgetTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task ApplicationLinksWidgetReturnsStatus()
        {
            // Arrange
            var applicationLinksService = Substitute.For<IApplicationLinksService>();
            var applicationId = Guid.NewGuid();
            var expectedCategory = "Category";
            var expectedReferenceNumber = "ReferenceNumber";
            var expectedStatus = "Status";
            List<ApplicationLinksInfoDto> applicationLinksInfoDtos = new List<ApplicationLinksInfoDto>();
            applicationLinksInfoDtos.Add(new ApplicationLinksInfoDto()
                {
                    ApplicationId = applicationId,
                    Category = expectedCategory,
                    ReferenceNumber = expectedReferenceNumber,
                    ApplicationStatus = expectedStatus
                });
            var httpContext = new DefaultHttpContext();

            applicationLinksService.GetListByApplicationAsync(applicationId).Returns(await Task.FromResult(applicationLinksInfoDtos));

            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ApplicationLinksWidgetViewComponent(applicationLinksService)
            {
                ViewComponentContext = viewComponentContext
            };

            //Act
            var result = await viewComponent.InvokeAsync(applicationId) as ViewViewComponentResult;
            ApplicationLinksWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ApplicationLinksWidgetViewModel;

            //Assert
            resultModel!.ApplicationLinks[0].Category.ShouldBe(expectedCategory);
            resultModel!.ApplicationLinks[0].ReferenceNumber.ShouldBe(expectedReferenceNumber);
            resultModel!.ApplicationLinks[0].ApplicationStatus.ShouldBe(expectedStatus);
        }
    }
}
