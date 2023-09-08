using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Comments;
using Xunit;

namespace Unity.GrantManager.GrantApplications
{
    public class ApplicationCommentsAppServiceTests : GrantManagerApplicationTestBase
    {        
        private readonly IGrantApplicationAppService _grantApplicationAppService;

        public ApplicationCommentsAppServiceTests()
        {            
            _grantApplicationAppService = GetRequiredService<IGrantApplicationAppService>();
        }

        protected override IServiceCollection CreateServiceCollection()
        {
            var serviceCollection = base.CreateServiceCollection();            
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
            _ = await _grantApplicationAppService.CreateCommentAsync(application.Id, new CreateCommentDto()
            {                 
                Comment = comment
            });

            // Assert
            (await _grantApplicationAppService.GetCommentsAsync(application.Id)).Any(s => s.Comment == comment).ShouldBe(true);            
        }
    }
}