using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class AssessmentScoresWidgetTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task AssessmentScoresWidgetReturnsStatus()
        {
            // Arrange
            var assessmentRepository = Substitute.For<IAssessmentRepository>();
            var scoresheetRepository = Substitute.For<IScoresheetRepository>();
            var instanceRepository = Substitute.For<IScoresheetInstanceRepository>();
            var expectedFinancialAnalysis = 1;
            var expectedEconomicImpact = 2;
            var expectedInclusiveGrowth = 3;
            var expectedCleanGrowth = 4;
            var assessmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            assessmentRepository.GetAsync(assessmentId).Returns(await Task.FromResult(new Assessment()
            {
                FinancialAnalysis = expectedFinancialAnalysis,
                EconomicImpact = expectedEconomicImpact,
                InclusiveGrowth = expectedInclusiveGrowth,
                CleanGrowth = expectedCleanGrowth
            }));

            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new AssessmentScoresWidgetViewComponent(assessmentRepository, scoresheetRepository, instanceRepository)
            {
                ViewComponentContext = viewComponentContext
            };

            //Act
            var result = await viewComponent.InvokeAsync(assessmentId, currentUserId) as ViewViewComponentResult;
            AssessmentScoresWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as AssessmentScoresWidgetViewModel;

            //Assert
            resultModel!.FinancialAnalysis.ShouldBe(expectedFinancialAnalysis);
            resultModel!.EconomicImpact.ShouldBe(expectedEconomicImpact);
            resultModel!.InclusiveGrowth.ShouldBe(expectedInclusiveGrowth);
            resultModel!.CleanGrowth.ShouldBe(expectedCleanGrowth);
        }
    }
}
