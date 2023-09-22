using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Comments;
using System;
using Volo.Abp.Validation;
using Unity.GrantManager.Exceptions;
using Unity.GrantManager.Assessments;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Repositories;

namespace Unity.GrantManager.GrantApplications;

public class ApplicationAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly IGrantApplicationAppService _grantApplicationAppService;
    private readonly IRepository<Application, Guid> _applicationsRepository;
    private readonly IRepository<ApplicationComment, Guid> _applicationCommentsRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ApplicationAppServiceTests()
    {
        _grantApplicationAppService = GetRequiredService<IGrantApplicationAppService>();
        _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
        _applicationCommentsRepository = GetRequiredService<IRepository<ApplicationComment, Guid>>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
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
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var applicationComments = (await _applicationCommentsRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList();
        var count = applicationComments.Count;
        var comment = "Test Application Comment Integration";

        // Act
        _ = await _grantApplicationAppService.CreateCommentAsync(application.Id, new CreateCommentDto()
        {
            Comment = comment
        });

        // Assert
        var afterAssessmentComments = (await _applicationCommentsRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList();
        afterAssessmentComments.Count.ShouldBe(count + 1);
    }


    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateCommentAsync_Should_Update_Comment()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];
        var applicationComment = (await _applicationCommentsRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList()[0];
        var updateComment = "Updated Comment";

        // Act
        var updatedCommentDto = await _grantApplicationAppService.UpdateCommentAsync(application.Id, new UpdateCommentDto()
        {
            CommentId = applicationComment.Id,
            Comment = updateComment
        });

        // Assert
        var updatedComment = await _applicationCommentsRepository.GetAsync(updatedCommentDto.Id);
        updatedComment.Comment.ShouldBe(updateComment);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetCommentListAsync_Should_Return_ApplicationComments()
    {
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];

        // Act
        var assessmentComments = (await _grantApplicationAppService.GetCommentsAsync(application.Id)).ToList();

        // Assert            
        assessmentComments.ShouldNotBeNull();
        assessmentComments.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateCommentAsync_Invalid_Should_Throw()
    {
        // Arrange                        
        // Act
        // Assert
        await Assert.ThrowsAsync<InvalidCommentParametersException>(() => _grantApplicationAppService.UpdateCommentAsync(Guid.NewGuid(), new UpdateCommentDto()
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
        await Assert.ThrowsAsync<InvalidCommentParametersException>(() => _grantApplicationAppService.GetCommentAsync(Guid.NewGuid(), Guid.NewGuid()));
    }
}
