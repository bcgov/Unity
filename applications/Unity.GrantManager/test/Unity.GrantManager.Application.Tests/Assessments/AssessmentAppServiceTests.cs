using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;
using Xunit;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IGrantApplicationAppService _grantApplicationAppService;
        private readonly IAssessmentAppService _assessmentAppService;

        public AssessmentAppServiceTests()
        {
            _assessmentAppService = GetRequiredService<IAssessmentAppService>();
            _grantApplicationAppService = GetRequiredService<IGrantApplicationAppService>();
        }

        protected override IServiceCollection CreateServiceCollection()
        {
            var serviceCollection = base.CreateServiceCollection();
            serviceCollection.AddTransient<IAssessmentAppService>();
            serviceCollection.AddTransient<IGrantApplicationAppService>();
            return serviceCollection;
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Should_Add_Integration_Test_Assessment()
        {
            // Arrange            
            var application = (await _grantApplicationAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto())).Items[0];

            // Act
            var assessment = await _assessmentAppService.CreateAsync(new CreateAssessmentDto()
            {
                ApplicationId = application.Id
            });

            // Assert
            var updatedAssessments = await _assessmentAppService.GetListAsync(application.Id);
            updatedAssessments.FirstOrDefault(s => s.Id == assessment.Id).ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Should_Add_AssessmentComment_To_Application()
        {
            // Arrange
            var application = (await _grantApplicationAppService.GetListAsync(
                    new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()))
                    .Items
                    .First(s => s.ProjectName == "Application For Integration Test Funding");

            var adjudication = (await _assessmentAppService.GetListAsync(application.Id))[0];
            var comment = "Test Assessment Comment Integration";

            // Act
            _ = await _assessmentAppService.CreateCommentAsync(adjudication.Id, new CreateCommentDto()
            {
                Comment = comment
            });

            // Assert
            var comments = await _assessmentAppService.GetCommentsAsync(adjudication.Id);
            comments.FirstOrDefault(s => s.Comment == comment).ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AssessmentComment_Should_Be_Updated()
        {
            // Arrange                        
            var application = (await _grantApplicationAppService.GetListAsync(
                    new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()))
                    .Items
                    .First(s => s.ProjectName == "Application For Integration Test Funding");

            var assessment = (await _assessmentAppService.GetListAsync(application.Id))[0];
            var comment = "Test Application Update Comment Integration";
            var updateComment = "Updated Comment";

            // Act
            var addedCommentDto = await _assessmentAppService.CreateCommentAsync(assessment.Id, new CreateCommentDto()
            {
                Comment = comment
            });

            var updatedCommentDto = await _assessmentAppService.UpdateCommentAsync(assessment.Id, new UpdateCommentDto()
            {
                CommentId = addedCommentDto.Id,
                Comment = updateComment
            });

            var updatedComment = await _assessmentAppService.GetCommentAsync(assessment.Id, updatedCommentDto.Id);

            // Assert
            updatedComment.Comment.ShouldBe(updateComment);
        }
    }
}
