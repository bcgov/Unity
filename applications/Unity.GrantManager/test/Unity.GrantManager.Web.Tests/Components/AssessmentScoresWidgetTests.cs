using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using NSubstitute;
using Shouldly;
using System;
using System.Threading.Tasks;
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
            var expectedSectionScore1 = 1;
            var expectedSectionScore2 = 2;
            var expectedSectionScore3 = 3;
            var expectedSectionScore4 = 4;
            var assessmentId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var httpContext = new DefaultHttpContext();
            assessmentRepository.GetAsync(assessmentId).Returns(await Task.FromResult(new Assessment()
            {
                SectionScore1 = expectedSectionScore1,
                SectionScore2 = expectedSectionScore2,
                SectionScore3 = expectedSectionScore3,
                SectionScore4 = expectedSectionScore4
            }));

            var viewContext = new ViewContext
            {
                HttpContext = httpContext
            };
            var viewComponentContext = new ViewComponentContext
            {
                ViewContext = viewContext
            };

            var viewComponent = new AssessmentScoresWidgetViewComponent(assessmentRepository)
            {
                ViewComponentContext = viewComponentContext
            };

            //Act
            var result = await viewComponent.InvokeAsync(assessmentId, currentUserId) as ViewViewComponentResult;
            AssessmentScoresWidgetViewModel? resultModel;

            resultModel = result!.ViewData!.Model! as AssessmentScoresWidgetViewModel;

            //Assert
            resultModel!.SectionScore1.ShouldBe(expectedSectionScore1);
            resultModel!.SectionScore2.ShouldBe(expectedSectionScore2);
            resultModel!.SectionScore3.ShouldBe(expectedSectionScore3);
            resultModel!.SectionScore4.ShouldBe(expectedSectionScore4);
        }
    }
}
