using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using Volo.Abp.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.Comments;
using Volo.Abp.Application.Services;
using Volo.Abp.Validation;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Assessments
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentAppService), typeof(IAssessmentAppService))]
    public class AssessmentAppService : ApplicationService, IAssessmentAppService
    {
        private readonly IAssessmentRepository _assessmentRepository;
        private readonly ICommentsManager _commentsManager;

        public AssessmentAppService(IAssessmentRepository assessmentRepository,
            ICommentsManager commentsManager)
        {
            _assessmentRepository = assessmentRepository;
            _commentsManager = commentsManager;
        }

        public async Task<AssessmentDto> CreateAsync(CreateAssessmentDto dto)
        {
            return ObjectMapper.Map<Assessment, AssessmentDto>(await _assessmentRepository.InsertAsync(
                new Assessment
                {
                    ApplicationId = dto.ApplicationId
                },
                autoSave: true
            ));
        }

        public async Task<IList<AssessmentDto>> GetListAsync(Guid applicationId)
        {
            IQueryable<Assessment> queryableAssessments = _assessmentRepository.GetQueryableAsync().Result;
            var comments = queryableAssessments.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<AssessmentDto>>(ObjectMapper.Map<List<Assessment>, List<AssessmentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
        }

        public async Task<CommentDto> CreateCommentAsync(Guid id, CreateCommentDto dto)
        {
            return ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)
             await _commentsManager.CreateCommentAsync(id, dto.Comment, CommentsManager.CommentType.AssessmentComment));
        }

        public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid id)
        {
            return ObjectMapper.Map<IReadOnlyList<AssessmentComment>, IReadOnlyList<CommentDto>>((IReadOnlyList<AssessmentComment>)
                await _commentsManager.GetCommentsAsync(id, CommentsManager.CommentType.AssessmentComment));
        }

        public async Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto)
        {
            try
            {
                return ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)
                      await _commentsManager.UpdateCommentAsync(id, dto.CommentId, dto.Comment, CommentsManager.CommentType.AssessmentComment));
            }
            catch (EntityNotFoundException)
            {
                throw new AbpValidationException("Comment not found");
            }
        }

        public async Task<CommentDto> GetCommentAsync(Guid id, Guid commentId)
        {
            var comment = await _commentsManager.GetCommentAsync(id, commentId, CommentsManager.CommentType.AssessmentComment);

            return comment == null
                ? throw new AbpValidationException("Comment not found")
                : ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)comment);
        }
    }
}
