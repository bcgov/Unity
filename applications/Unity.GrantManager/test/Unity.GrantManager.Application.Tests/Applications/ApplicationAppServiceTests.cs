using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Comments;
using System;
using Volo.Abp.Validation;

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
    public async Task GetListAsync_Should_Return_Items()
    {        
        // Act
        var grantApplications = await _grantApplicationAppService.GetListAsync(new Volo.Abp.Application.Dtos.PagedAndSortedResultRequestDto() { MaxResultCount = 100 });

        // Assert
        grantApplications.Items.Any(s => s.ProjectName == "Application For Integration Test Funding").ShouldBeTrue();        
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateCommentAsync_Should_Create_Comment()
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
    public async Task UpdateCommentAsync_Should_Update_Comment()
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateCommentAsync_Invalid_Should_Throw()
    {
        // Arrange                        
        // Act
        // Assert
        await Should.ThrowAsync<AbpValidationException>(_grantApplicationAppService.UpdateCommentAsync(Guid.NewGuid(), new UpdateCommentDto()
        {
            CommentId = Guid.NewGuid(),
            Comment = "Foobar"
        }));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCommentAsync_Invalid_Should_Throw()
    {
        // Arrange                        
        // Act
        // Assert
        await Should.ThrowAsync<AbpValidationException>(_grantApplicationAppService.GetCommentAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
