using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo;
using Unity.GrantManager.Locality;
using Volo.Abp.DependencyInjection;
using Xunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Unity.GrantManager.Components
{
    [Collection(WebTestCollection.Name)]
    public class ProjectInfoWidgetTests
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;
        private readonly IAuthorizationService authorizationService;

        public ProjectInfoWidgetTests(WebTestFixture fixture)
        {
            lazyServiceProvider = fixture.Services.GetRequiredService<IAbpLazyServiceProvider>();
            authorizationService = fixture.Services.GetRequiredService<IAuthorizationService>();
        }

        [Fact]
        public async Task ContactInfoReturnsStatus()
        {
            var applicationDto = new GrantApplicationDto()
            {
                ProjectName = "Unity",
                ProjectSummary = "Grant",
                RequestedAmount = 123456,
                TotalProjectBudget = 50,
            };

            // Arrange
            var appService = Substitute.For<IGrantApplicationAppService>();
            appService.GetAsync(Arg.Any<Guid>()).Returns(applicationDto);
            var economicRegionService = Substitute.For<IEconomicRegionService>();
            var electoralDistrictService = Substitute.For<IElectoralDistrictService>();
            var regionalDistrictService = Substitute.For<IRegionalDistrictService>();
            var communitiesService = Substitute.For<ICommunityService>();
            var viewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new ProjectInfoViewComponent(appService, economicRegionService, electoralDistrictService, regionalDistrictService, communitiesService, this.authorizationService)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            //Act
            var result = await viewComponent.InvokeAsync(Guid.NewGuid(), Guid.NewGuid()) as ViewViewComponentResult;
            ProjectInfoViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ProjectInfoViewModel;

            //Assert

            var expectedProjectName = "Unity";
            var expectedProjectSummary = "Grant";
            var expectedRequestedAmount = 123456;
            var expectedTotalProjectBudget = 50;
          

            resultModel!.ProjectInfo!.ProjectName.ShouldBe(expectedProjectName);
            resultModel!.ProjectInfo!.ProjectSummary.ShouldBe(expectedProjectSummary);
            resultModel!.ProjectInfo!.RequestedAmount.ShouldBe(expectedRequestedAmount);
            resultModel!.ProjectInfo!.TotalProjectBudget.ShouldBe(expectedTotalProjectBudget);
        }
    }
}
