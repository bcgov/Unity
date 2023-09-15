using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Unity.GrantManager.Assessments;
using System.Linq;
using System;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Exceptions;

namespace Unity.GrantManager.Comments
{
    public class CommentAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly ICommentAppService _commentsAppService;
        private readonly IRepository<Application, Guid> _applicationsRepository;
        private readonly IRepository<ApplicationComment, Guid> _applicationCommentsRepository;
        private readonly IRepository<Assessment, Guid> _assessmentsRepository;
        private readonly IRepository<AssessmentComment, Guid> _assessmentCommentsRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public CommentAppServiceTests()
        {
            _commentsAppService = GetRequiredService<ICommentAppService>();
            _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
            _applicationCommentsRepository = GetRequiredService<IRepository<ApplicationComment, Guid>>();
            _assessmentsRepository = GetRequiredService<IRepository<Assessment, Guid>>();
            _assessmentCommentsRepository = GetRequiredService<IRepository<AssessmentComment, Guid>>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListAsync_Should_Return_ApplicationComments()
        {
            // Arrange
            var application = (await _applicationsRepository.GetListAsync())[0];

            // Act
            var applicationComments = (await _commentsAppService.GetListAsync(new QueryCommentsByTypeDto()
            {
                CommentType = CommentType.ApplicationComment,
                OwnerId = application.Id
            })).ToList();

            // Assert            
            applicationComments.ShouldNotBeNull();
            applicationComments.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetListAsync_Should_Return_AssessmentComments()
        {
            // Arrange            
            var assessment = (await _assessmentsRepository.GetListAsync())[0];

            // Act
            var assessmentComments = (await _commentsAppService.GetListAsync(new QueryCommentsByTypeDto()
            {
                CommentType = CommentType.AssessmentComment,
                OwnerId = assessment.Id
            })).ToList();

            // Assert            
            assessmentComments.ShouldNotBeNull();
            assessmentComments.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateAsync_Should_Create_ApplicationComment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var application = (await _applicationsRepository.GetListAsync())[0];
            var comment = "Test Add ApplicationComment Through Comment Api";

            // Act
            var addedComment = await _commentsAppService.CreateAsync(new CreateCommentByTypeDto()
            {
                Comment = comment,
                CommentType = CommentType.ApplicationComment,
                OwnerId = application.Id
            });

            // Assert
            (await _applicationCommentsRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.Id == addedComment.Id && s.Comment == comment)
                .ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateAsync_Should_Create_AssessmentComment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var assessment = (await _assessmentsRepository.GetListAsync())[0];
            var comment = "Test Add AssessmentComment Through Comment Api";

            // Act
            var addedComment = await _commentsAppService.CreateAsync(new CreateCommentByTypeDto()
            {
                Comment = comment,
                CommentType = CommentType.AssessmentComment,
                OwnerId = assessment.Id
            });

            // Assert
            (await _assessmentCommentsRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.Id == addedComment.Id && s.Comment == comment)
                .ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateAsync_Should_Update_AssessmentComment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var assessment = (await _assessmentsRepository.GetListAsync())[0];
            var assessmentComment = (await _assessmentCommentsRepository.GetQueryableAsync())
                    .FirstOrDefault(s => s.AssessmentId == assessment.Id);
            var udpateComment = assessmentComment!.Comment + " updated";

            // Act
            var updatedComment = await _commentsAppService.UpdateAsync(new UpdateCommentByTypeDto()
            {
                CommentId = assessmentComment.Id,
                Comment = udpateComment,
                CommentType = CommentType.AssessmentComment,
                OwnerId = assessment.Id
            });

            // Assert
            (await _assessmentCommentsRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.Id == updatedComment.Id && s.Comment == udpateComment)
                .ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateAsync_Should_Update_ApplicationComment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var application = (await _applicationsRepository.GetListAsync())[0];
            var applicationComment = (await _applicationCommentsRepository.GetQueryableAsync())
                    .FirstOrDefault(s => s.ApplicationId == application.Id);
            var udpateComment = applicationComment!.Comment + " updated";

            // Act
            var updatedComment = await _commentsAppService.UpdateAsync(new UpdateCommentByTypeDto()
            {
                CommentId = applicationComment.Id,
                Comment = udpateComment,
                CommentType = CommentType.ApplicationComment,
                OwnerId = application.Id
            });

            // Assert
            (await _applicationCommentsRepository.GetQueryableAsync())
                .FirstOrDefault(s => s.Id == updatedComment.Id && s.Comment == udpateComment)
                .ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAsync_Should_Get_ApplicationComment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var application = (await _applicationsRepository.GetListAsync())[0];
            var applicationComment = (await _applicationCommentsRepository.GetQueryableAsync())
                    .FirstOrDefault(s => s.ApplicationId == application.Id);

            // Act
            var comment = await _commentsAppService.GetAsync(applicationComment!.Id, new QueryCommentsByTypeDto()
            {
                CommentType = CommentType.ApplicationComment,
                OwnerId = application.Id
            });

            // Assert
            comment.ShouldNotBeNull();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetAsync_Should_Get_AssessmentComment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var assessment = (await _applicationsRepository.GetListAsync())[0];
            var assessmentComment = (await _applicationCommentsRepository.GetQueryableAsync())
                    .FirstOrDefault(s => s.ApplicationId == assessment.Id);

            // Act
            var comment = await _commentsAppService.GetAsync(assessmentComment!.Id, new QueryCommentsByTypeDto()
            {
                CommentType = CommentType.ApplicationComment,
                OwnerId = assessment.Id
            });

            // Assert
            comment.ShouldNotBeNull();
        }

        [Theory]
        [InlineData(CommentType.AssessmentComment)]
        [InlineData(CommentType.ApplicationComment)]
        [Trait("Category", "Integration")]
        public async Task UpdateCommentAsync_Invalid_Should_Throw(CommentType commentType)
        {
            // Arrange                        
            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidCommentParametersException>(() => _commentsAppService.UpdateAsync(new UpdateCommentByTypeDto()
            {
                Comment = "Foo",
                CommentId = Guid.NewGuid(),
                CommentType = commentType,
                OwnerId = Guid.NewGuid()
            }));
        }


        [Theory]
        [InlineData(CommentType.AssessmentComment)]
        [InlineData(CommentType.ApplicationComment)]
        [Trait("Category", "Integration")]
        public async Task GetCommentAsync_Invalid_Should_Throw(CommentType commentType)
        {
            // Arrange                        
            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidCommentParametersException>(() => _commentsAppService.GetAsync(Guid.NewGuid(), new QueryCommentsByTypeDto()
            {
                CommentType = commentType,
                OwnerId = Guid.NewGuid()
            }));
        }
    }
}
