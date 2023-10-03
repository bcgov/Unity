using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Xunit;
using Shouldly;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentCommentsAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IAssessmentCommentsService _assessmentsCommentsService;
        private readonly IAssessmentAppService _assessmentsService;
        private readonly IGrantApplicationAppService _grantApplicationAppService;

        public AssessmentCommentsAppServiceTests()
        {
            _assessmentsService = GetRequiredService<IAssessmentAppService>();
            _assessmentsCommentsService = GetRequiredService<IAssessmentCommentsService>();
            _grantApplicationAppService = GetRequiredService<IGrantApplicationAppService>();
        }

        protected override IServiceCollection CreateServiceCollection()
        {
            var serviceCollection = base.CreateServiceCollection();
            serviceCollection.AddTransient<IAssessmentAppService>();
            serviceCollection.AddTransient<IAssessmentCommentsService>();
            serviceCollection.AddTransient<IGrantApplicationAppService>();
            return serviceCollection;
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Should_Add_Assessment_To_Application()
        {
            // Arrange
            var application = (await _grantApplicationAppService.GetListAsync(
                    new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()))
                    .Items
                    .First(s => s.ProjectName == "Application For Integration Test Funding");

            var assessment = (await _assessmentsService.GetListAsync(application.Id))[0];      
            var comment = "Test Assessment Comment Integration";

            // Act
            _ = await _assessmentsCommentsService.CreateAssessmentComment(new CreateAssessmentCommentDto()
            {
                AssessmentId = assessment.Id,
                Comment = comment
            });

            // Assert
            var comments = await _assessmentsCommentsService.GetListAsync(assessment.Id);
            comments.FirstOrDefault(s => s.Comment == comment).ShouldNotBeNull();            
        }
    }
}
