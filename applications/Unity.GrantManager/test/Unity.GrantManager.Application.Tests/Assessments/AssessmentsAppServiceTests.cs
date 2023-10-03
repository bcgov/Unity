using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Xunit;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentsAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IGrantApplicationAppService _grantApplicationAppService;
        private readonly IAssessmentAppService _assessmentsService;

        public AssessmentsAppServiceTests()
        {
            _assessmentsService = GetRequiredService<IAssessmentAppService>();
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
            var assessment = await _assessmentsService.CreateAssessment(new CreateAssessmentDto()
            {
                ApplicationId = application.Id
            });

            // Assert
            var updatedAssessments = await _assessmentsService.GetListAsync(application.Id);
            updatedAssessments.FirstOrDefault(s => s.Id == assessment.Id).ShouldNotBeNull();
        }
    }
}
