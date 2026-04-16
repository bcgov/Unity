using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Web.Views.Shared.Components.AssessmentScoresWidget;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Xunit;

namespace Unity.GrantManager.Components
{
    [Collection(WebTestCollection.Name)]
    public class AssessmentScoresWidgetTests
    {
        [Fact]
        public async Task AssessmentScoresWidgetReturnsStatus()
        {
            // Arrange
            var assessmentRepository = Substitute.For<IAssessmentRepository>();
            var scoresheetRepository = Substitute.For<IScoresheetRepository>();
            var instanceRepository = Substitute.For<IScoresheetInstanceRepository>();
            var applicationRepository = Substitute.For<IApplicationRepository>();
            var featureChecker = Substitute.For<IFeatureChecker>();
            var permissionChecker = Substitute.For<IPermissionChecker>();
            var expectedFinancialAnalysis = 1;
            var expectedEconomicImpact = 2;
            var expectedInclusiveGrowth = 3;
            var expectedCleanGrowth = 4;
            var assessmentId = Guid.NewGuid();
            var applicationId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();

            assessmentRepository.GetAsync(assessmentId).Returns(Task.FromResult(new Assessment(id: assessmentId, applicationId: applicationId, assessorId: Guid.NewGuid())
            {
                FinancialAnalysis = expectedFinancialAnalysis,
                EconomicImpact = expectedEconomicImpact,
                InclusiveGrowth = expectedInclusiveGrowth,
                CleanGrowth = expectedCleanGrowth
            }));

            applicationRepository.GetAsync(applicationId).Returns(Task.FromResult(new Application()
            {
                AIScoresheetAnswers = null
            }));

            instanceRepository.GetByCorrelationAsync(assessmentId).Returns(Task.FromResult<ScoresheetInstance?>(null));
            featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(Task.FromResult(true));
            permissionChecker.IsGrantedAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new AssessmentScoresWidgetViewComponent(
                assessmentRepository,
                scoresheetRepository,
                instanceRepository,
                applicationRepository,
                featureChecker,
                permissionChecker)
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
