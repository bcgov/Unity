using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Comments;

namespace Unity.GrantManager.GrantApplications;

public class ApplicationAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly IGrantApplicationAppService _grantApplicationAppService;

    public ApplicationAppServiceTests()
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
    public async Task Should_Add_Integration_Test_Application()
    {        
        // Act
        var grantApplications = await _grantApplicationAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto() { MaxResultCount = 100 });

        // Assert
        grantApplications.Items.Any(s => s.ProjectName == "Application For Integration Test Funding").ShouldBeTrue();        
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


    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplicationComment_Should_Be_Updated()
    {
        // Arrange
        var application = (await _grantApplicationAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto())).Items[0];
        var comment = "Test Application Update Comment Integration";
        var updateComment = "Updated Comment";

        // Act
        var addedCommentDto = await _grantApplicationAppService.CreateCommentAsync(application.Id, new CreateCommentDto()
        {
            Comment = comment
        });

        var updatedCommentDto = await _grantApplicationAppService.UpdateCommentAsync(application.Id, new UpdateCommentDto()
        {
            CommentId = addedCommentDto.Id,
            Comment = updateComment
        });

        var updatedComment = await _grantApplicationAppService.GetCommentAsync(application.Id, updatedCommentDto.Id);

        // Assert
        updatedComment.Comment.ShouldBe(updateComment);
    }
}
