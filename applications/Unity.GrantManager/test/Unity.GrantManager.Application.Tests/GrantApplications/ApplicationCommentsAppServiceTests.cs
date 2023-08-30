using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Xunit;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationCommentsAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IApplicationCommentsService _applicationCommentsService;
        private readonly IGrantApplicationAppService _grantApplicationAppService;

        public ApplicationCommentsAppServiceTests()
        {
            _applicationCommentsService = GetRequiredService<IApplicationCommentsService>();
            _grantApplicationAppService = GetRequiredService<IGrantApplicationAppService>();
        }

        protected override IServiceCollection CreateServiceCollection()
        {
            var serviceCollection = base.CreateServiceCollection();
            serviceCollection.AddTransient<IApplicationCommentsService>();
            serviceCollection.AddTransient<IGrantApplicationAppService>();
            return serviceCollection;
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Comment_Should_Be_Added_To_Application()
        {            
            // Arrange
            var application = (await _grantApplicationAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto())).Items[0];
            var comment = "Test Application Comment Integration";

            // Act
            _ = await _applicationCommentsService.CreateApplicationComment(new CreateApplicationCommentDto()
            { 
                ApplicationId = application.Id,
                Comment = comment
            });

            // Assert
            (await _applicationCommentsService.GetListAsync(application.Id)).Any(s => s.Comment == comment).ShouldBe(true);            
        }
    }
}