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

namespace Unity.GrantManager.Components
{
    public class ProjectInfoWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public ProjectInfoWidgetTests()
        {
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();
        }

        [Fact(Skip = "Failing Test")]
        public async Task ContactInfoReturnsStatus()
        {
            var applicationDto = new GrantApplicationDto()
            {
                ContactFullName = "John Doe",
                ContactTitle = "Doctor",
                ContactEmail = "john.doe@email.com",
                ContactBusinessPhone = "+12501234567",
                ContactCellPhone = "+12501234567"
            };

            // Arrange
            var appService = Substitute.For<IGrantApplicationAppService>();
            appService.GetAsync(Arg.Any<Guid>()).Returns(applicationDto);
            var sectorService = Substitute.For<ISectorService>();
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

            var viewComponent = new ProjectInfoViewComponent(appService, sectorService, economicRegionService, electoralDistrictService, regionalDistrictService, communitiesService)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            //Act
            var result = await viewComponent.InvokeAsync(Guid.NewGuid()) as ViewViewComponentResult;
            ProjectInfoViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as ProjectInfoViewModel;

            //Assert

            var expectedFullName = "John Doe";
            var expectedTitle = "Doctor";
            var expectedEmail = "john.doe@email.com";
            var expectedBusinessPhone = "+12501234567";
            var expectedCellPhone = "+12501234567";

            resultModel!.ProjectInfo!.ContactFullName.ShouldBe(expectedFullName);
            resultModel!.ProjectInfo!.ContactTitle.ShouldBe(expectedTitle);
            resultModel!.ProjectInfo!.ContactEmail.ShouldBe(expectedEmail);
            resultModel!.ProjectInfo!.ContactBusinessPhone.ShouldBe(expectedBusinessPhone);
            resultModel!.ProjectInfo!.ContactCellPhone.ShouldBe(expectedCellPhone);
        }
    }
}
