using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Web.Views.Shared.Components.Summary;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class SummaryWidgetTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task SummaryWidgetReturnsStatus()
        {
            // Arrange
            var applicationRepository = Substitute.For<IApplicationRepository>();
            var applicationId = Guid.NewGuid();
            var appFormId = Guid.NewGuid();
            var expectedCategory = "Banking";
            var expectedSubmissionDate = DateTime.UtcNow;
            var expectedEconomicRegion = "Canada";
            var expectedCity = "Victoria";
            var expectedRequestedAmount = 10000;
            var expectedProjectBudget = 20000;
            var expectedSector = "Information Technology";
            var expectedStatus = "Grant Approved";
            var expectedLikelihoodOfFunding = "LOW";
            var expectedAssessmentStartDate = DateTime.UtcNow;
            var expectedFinalDecisionDate = DateTime.UtcNow;
            var expectedTotalScore = 100;
            var expectedAssessmentResult = "FAIL";
            var expectedRecommendedAmount = 8000;
            var expectedApprovedAmount = 5000;
            var httpContext = new DefaultHttpContext();
            applicationRepository.GetAsync(applicationId).Returns(await Task.FromResult(new Application()
            {
                ApplicationFormId = appFormId,
                CreationTime = expectedSubmissionDate,
                EconomicRegion = expectedEconomicRegion,
                City = expectedCity,
                RequestedAmount = expectedRequestedAmount,
                TotalProjectBudget = expectedProjectBudget,
                Sector = expectedSector,
                ApplicationStatus = new ApplicationStatus() { InternalStatus = expectedStatus },
                LikelihoodOfFunding = expectedLikelihoodOfFunding,
                AssessmentStartDate = expectedAssessmentStartDate,
                FinalDecisionDate = expectedFinalDecisionDate,
                TotalScore = expectedTotalScore,
                AssessmentResultStatus = expectedAssessmentResult,
                RecommendedAmount = expectedRecommendedAmount,
                ApprovedAmount = expectedApprovedAmount,
            }));
            var appFormRepository = Substitute.For<IApplicationFormRepository>();
            appFormRepository.GetAsync(appFormId).Returns(await Task.FromResult(new ApplicationForm()
            {
                Category = expectedCategory,
            }));
            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new SummaryWidgetViewComponent(applicationRepository, appFormRepository)
            {
                ViewComponentContext = viewComponentContext
            };

            //Act
            var result = await viewComponent.InvokeAsync(applicationId) as ViewViewComponentResult;
            SummaryWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as SummaryWidgetViewModel;

            //Assert
            resultModel!.SubmissionDate.ShouldBe(expectedSubmissionDate.ToShortDateString());
            resultModel!.EconomicRegion.ShouldBe(expectedEconomicRegion);
            resultModel!.City.ShouldBe(expectedCity);
            resultModel!.RequestedAmount.ShouldBe(string.Format(new CultureInfo("en-CA"), "{0:C}", expectedRequestedAmount));
            resultModel!.ProjectBudget.ShouldBe(string.Format(new CultureInfo("en-CA"), "{0:C}", expectedProjectBudget));
            resultModel!.Sector.ShouldBe(expectedSector);
            resultModel!.Status.ShouldBe(expectedStatus);
            resultModel!.LikelihoodOfFunding.ShouldBe(expectedLikelihoodOfFunding);
            resultModel!.AssessmentStartDate.ShouldBe(expectedAssessmentStartDate.ToShortDateString());
            resultModel!.FinalDecisionDate.ShouldBe(expectedFinalDecisionDate.ToShortDateString());
            resultModel!.TotalScore.ShouldBe(expectedTotalScore.ToString());
            resultModel!.AssessmentResult.ShouldBe(expectedAssessmentResult);
            resultModel!.RecommendedAmount.ShouldBe(string.Format(new CultureInfo("en-CA"), "{0:C}", expectedRecommendedAmount));
            resultModel!.ApprovedAmount.ShouldBe(string.Format(new CultureInfo("en-CA"), "{0:C}",expectedApprovedAmount));
        }
    }
}
