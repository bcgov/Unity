using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Exceptions;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.GrantManager.Assessments
{
    public class AssessmentAppServiceTests : GrantManagerApplicationTestBase
    {
        private readonly IAssessmentAppService _assessmentAppService;
        private readonly IRepository<Application, Guid> _applicationsRepository;
        private readonly IRepository<Assessment, Guid> _assessmentRepository;
        private readonly IRepository<AssessmentComment, Guid> _assessmentCommentRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public AssessmentAppServiceTests()
        {
            _assessmentAppService = GetRequiredService<IAssessmentAppService>();
            _applicationsRepository = GetRequiredService<IRepository<Application, Guid>>();
            _assessmentRepository = GetRequiredService<IRepository<Assessment, Guid>>();
            _assessmentCommentRepository = GetRequiredService<IRepository<AssessmentComment, Guid>>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateAsync_Should_Create_Assessment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();            
            var application = (await _applicationsRepository.GetListAsync())[0];
            var assessments = (await _assessmentRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList();
            var count = assessments.Count;

            // Act
            _ = await _assessmentAppService.CreateAsync(new CreateAssessmentDto()
            {
                ApplicationId = application.Id
            });

            // Assert            
            var afterAssessments = (await _assessmentRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList();
            afterAssessments.Count.ShouldBe(count + 1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task GetCommentListAsync_Should_Return_AssessmentComments()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var application = (await _applicationsRepository.GetListAsync())[0];
            var assessment = (await _assessmentRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList()[0];

            // Act
            var assessmentComments = (await _assessmentAppService.GetCommentsAsync(assessment.Id)).ToList();

            // Assert            
            assessmentComments.ShouldNotBeNull();
            assessmentComments.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task CreateCommentAsync_Should_Create_Comment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin();
            var application = (await _applicationsRepository.GetListAsync())[0];
            var assessment = (await _assessmentRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList()[0];
            var assessmentComments = (await _assessmentCommentRepository.GetQueryableAsync()).Where(s => s.AssessmentId == assessment.Id).ToList();
            var count = assessmentComments.Count;
            var comment = "Test Assessment Comment Integration";

            // Act
            _ = await _assessmentAppService.CreateCommentAsync(assessment.Id, new CreateCommentDto()
            {
                Comment = comment
            });

            // Assert
            var afterAssessmentComments = (await _assessmentCommentRepository.GetQueryableAsync()).Where(s => s.AssessmentId == assessment.Id).ToList();
            afterAssessmentComments.Count.ShouldBe(count + 1);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateCommentAsync_Should_Update_Comment()
        {
            // Arrange
            using var uow = _unitOfWorkManager.Begin(); 
            var application = (await _applicationsRepository.GetListAsync())[0];
            var assessment = (await _assessmentRepository.GetQueryableAsync()).Where(s => s.ApplicationId == application.Id).ToList()[0];
            var assessmentComment = (await _assessmentCommentRepository.GetQueryableAsync()).Where(s => s.AssessmentId == assessment.Id).ToList()[0];
            var updateComment = "Updated Comment";

            // Act
            var updatedCommentDto = await _assessmentAppService.UpdateCommentAsync(assessment.Id, new UpdateCommentDto()
            {
                CommentId = assessmentComment.Id,
                Comment = updateComment
            });

            // Assert
            var updatedComment = await _assessmentCommentRepository.GetAsync(updatedCommentDto.Id);
            updatedComment.Comment.ShouldBe(updateComment);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UpdateCommentAsync_Invalid_Should_Throw()
        {
            // Arrange                        
            // Act
            // Assert
            await Assert.ThrowsAsync<InvalidCommentParametersException>(() => _assessmentAppService.UpdateCommentAsync(Guid.NewGuid(), new UpdateCommentDto()
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
            await Assert.ThrowsAsync<InvalidCommentParametersException>(() => _assessmentAppService.GetCommentAsync(Guid.NewGuid(), Guid.NewGuid()));
        }
    }
}
