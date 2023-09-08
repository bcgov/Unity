using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Xunit;
using Shouldly;
using Unity.GrantManager.Comments;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentCommentsAppServiceTests : GrantManagerApplicationTestBase
    {        
        private readonly IAssessmentAppService _adjuductionAppService;
        private readonly IGrantApplicationAppService _grantApplicationAppService;

        public AssessmentCommentsAppServiceTests()
        {
            _adjuductionAppService = GetRequiredService<IAssessmentAppService>();     
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
        public async Task Should_Add_Assessment_To_Application()
        {
            // Arrange
            var application = (await _grantApplicationAppService.GetListAsync(
                    new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto()))
                    .Items
                    .First(s => s.ProjectName == "Application For Integration Test Funding");

            var adjudication = (await _adjuductionAppService.GetListAsync(application.Id))[0];      
            var comment = "Test Assessment Comment Integration";

            // Act
            _ = await _adjuductionAppService.CreateCommentAsync(adjudication.Id, new CreateCommentDto()
            {                
                Comment = comment
            });

            // Assert
            var comments = await _adjuductionAppService.GetCommentsAsync(adjudication.Id);
            comments.FirstOrDefault(s => s.Comment == comment).ShouldNotBeNull();            
        }
    }
}
