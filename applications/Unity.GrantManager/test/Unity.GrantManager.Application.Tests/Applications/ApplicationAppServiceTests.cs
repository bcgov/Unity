using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Uow;
using Volo.Abp.Users;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications;

public class ApplicationAppServiceTests : GrantManagerApplicationTestBase
{
    private readonly GrantApplicationAppService _grantApplicationAppServiceTest;
    private readonly IGrantApplicationAppService _grantApplicationAppService;
    private readonly IRepository<Application, Guid> _applicationsRepository;
    private readonly IRepository<ApplicationComment, Guid> _applicationCommentsRepository;
    private readonly IApplicationAssignmentRepository _userAssignmentRepository;
    private readonly IIdentityUserIntegrationService _identityUserLookupAppService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ApplicationAppServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {

        _grantApplicationAppServiceTest = GetRequiredService<GrantApplicationAppService>();
        _grantApplicationAppService = GetRequiredService<IGrantApplicationAppService>();
        _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
        _applicationCommentsRepository = GetRequiredService<IRepository<ApplicationComment, Guid>>();
        _identityUserLookupAppService = GetRequiredService<IIdentityUserIntegrationService>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        _userAssignmentRepository = GetRequiredService<IApplicationAssignmentRepository>();
    }

    [Fact]
    public async Task AddRemoveAssigneeAsync_Should_AddRemove_Assignee()
    {        
        // Arrange
        using var uow = _unitOfWorkManager.Begin();
        var application = (await _applicationsRepository.GetListAsync())[0];        

        var users = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
        if (users != null && users.Items.Count > 0)
        {
            UserData uData = users.Items[0];

            // Act
            await _grantApplicationAppServiceTest.InsertAssigneeAsync(application.Id, uData.Id,"");
            await uow.SaveChangesAsync();


            // Assert
            IQueryable<ApplicationAssignment> queryableAssignment = await _userAssignmentRepository.GetQueryableAsync();
            var assignments = queryableAssignment.ToList();
            assignments.Count.ShouldBe(1);

            // Act
            await _grantApplicationAppServiceTest.DeleteAssigneeAsync(application.Id, uData.Id);
            await uow.SaveChangesAsync();

            IQueryable<ApplicationAssignment> queryableAssignment2 = await _userAssignmentRepository.GetQueryableAsync();
            var assignments2 = queryableAssignment2.ToList();
            assignments2.Count.ShouldBe(0);
        }
    }


    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateCommentAsync_Should_Create_Comment()
    {
        // Arrange
        Login(GrantManagerTestData.User1_UserId);

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
