using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using Volo.Abp.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using Unity.GrantManager.Comments;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Unity.GrantManager.Exceptions;

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

        public async Task UpdateAssessmentRecommendation(UpdateAssessmentRecommendationDto dto)
        {            
            var assessment = await _assessmentRepository.GetAsync(dto.AssessmentId);
            if (assessment != null)
            {
                assessment.ApprovalRecommended = dto.ApprovalRecommended;
                await _assessmentRepository.UpdateAsync(assessment);
            }
        }

        public async Task<CommentDto> CreateCommentAsync(Guid id, CreateCommentDto dto)
        {
            return ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)
             await _commentsManager.CreateCommentAsync(id, dto.Comment, CommentType.AssessmentComment));
        }

        public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid id)
        {
            return ObjectMapper.Map<IReadOnlyList<AssessmentComment>, IReadOnlyList<CommentDto>>((IReadOnlyList<AssessmentComment>)
                await _commentsManager.GetCommentsAsync(id, CommentType.AssessmentComment));
        }

        public async Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto)
        {
            try
            {
                return ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)
                      await _commentsManager.UpdateCommentAsync(id, dto.CommentId, dto.Comment, CommentType.AssessmentComment));
            }
            catch (EntityNotFoundException)
            {
                throw new InvalidCommentParametersException();
            }
        }

        public async Task<CommentDto> GetCommentAsync(Guid id, Guid commentId)
        {
            var comment = await _commentsManager.GetCommentAsync(id, commentId, CommentType.AssessmentComment);

            return comment == null
                ? throw new InvalidCommentParametersException()
                : ObjectMapper.Map<AssessmentComment, CommentDto>((AssessmentComment)comment);
        }
    }
}
