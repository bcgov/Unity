using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Web.Services;
using Unity.GrantManager.Web.Views.Shared.Components.Summary;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class SummaryWidgetTests : GrantManagerWebTestBase
    {
        private readonly IAbpLazyServiceProvider lazyServiceProvider;

        public SummaryWidgetTests()
        {            
            lazyServiceProvider = GetRequiredService<IAbpLazyServiceProvider>();            
        }

        [Fact]
        public async Task SummaryWidgetReturnsStatus()
        {
            var summaryDto = new GetSummaryDto()
            {
                ApprovedAmount = 1000,
                AssessmentResult = "FAIL",
                AssessmentStartDate = DateTime.UtcNow.ToString(),
                Batch = string.Empty,
                Category = "Banking",
                City = "Victoria",
                Community = string.Empty,
                EconomicRegion = "Canada",
                FinalDecisionDate = DateTime.UtcNow.ToString(),
                LikelihoodOfFunding = "LOW",
                OrganizationName = string.Empty,
                OrganizationNumber = string.Empty,
                ProjectBudget = 1000,
                RecommendedAmount = 10000,
                RequestedAmount = 1000,
                Sector = "Information Technology",
                Status = "Approved",
                SubmissionDate = DateTime.UtcNow,
                TotalScore = "100"
            };

            // Arrange
            var appService = Substitute.For<IGrantApplicationAppService>();
            var context = Substitute.For<IHttpContextAccessor>();
            var browserUtils = new BrowserUtils(context);
            appService.GetSummaryAsync(Arg.Any<Guid>()).Returns(summaryDto);

            var viewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new SummaryWidgetViewComponent(appService, browserUtils)
            {
                ViewComponentContext = viewComponentContext,
                LazyServiceProvider = lazyServiceProvider
            };

            //Act
            var result = await viewComponent.InvokeAsync(Guid.NewGuid(), true) as ViewViewComponentResult;
            SummaryWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as SummaryWidgetViewModel;

            //Assert

            var expectedSector = "Information Technology";
            var expectedStatus = "Approved";
            var expectedLikelihoodOfFunding = "LOW";
            var expectedTotalScore = 100;
            var expectedAssessmentResult = "FAIL";
            
            resultModel!.Sector.ShouldBe(expectedSector);
            resultModel!.Status.ShouldBe(expectedStatus);
            resultModel!.LikelihoodOfFunding.ShouldBe(expectedLikelihoodOfFunding);
            resultModel.TotalScore.ShouldBe(expectedTotalScore);
            resultModel!.AssessmentResult.ShouldBe(expectedAssessmentResult);
        }
    }
}
