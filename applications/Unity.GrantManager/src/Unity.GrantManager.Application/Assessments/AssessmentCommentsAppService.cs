using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Assessments
{

    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentCommentsService), typeof(IAssessmentCommentsService))]
    public class AssessmentCommentsService : ApplicationService, IAssessmentCommentsService
    {
        private readonly IAssessmentCommentsRepository _assessmentCommentsRepository;

        public AssessmentCommentsService(IAssessmentCommentsRepository assessmentCommentsRepository)
        {
            _assessmentCommentsRepository = assessmentCommentsRepository;
        }

        public async Task<IList<AssessmentCommentDto>> GetListAsync(Guid assessmentId)
        {
            IQueryable<AssessmentComment> queryableComment = _assessmentCommentsRepository.GetQueryableAsync().Result;
            var comments = queryableComment.Where(c => c.AssessmentId.Equals(assessmentId)).ToList();
            return await Task.FromResult<IList<AssessmentCommentDto>>(ObjectMapper.Map<List<AssessmentComment>, List<AssessmentCommentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
        }


        public async Task<AssessmentCommentDto> CreateAssessmentComment(CreateAssessmentCommentDto dto)
        {
            try
            {
                return ObjectMapper.Map<AssessmentComment, AssessmentCommentDto>(
                    await _assessmentCommentsRepository.InsertAsync(
                    new AssessmentComment
                    {
                        Comment = dto.Comment,
                        AssessmentId = dto.AssessmentId,
                    },
                    autoSave: true)
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating assessment comment");
                return new AssessmentCommentDto();
                // TODO: Exception handling
            }
        }

        public async Task UpdateAssessmentComment(UpdateAssessmentCommentDto dto)
        {
            try
            {
                var comment = await _assessmentCommentsRepository.GetAsync(dto.CommentId);
                if (comment != null)
                {
                    comment.Comment = dto.Comment;
                    await _assessmentCommentsRepository.UpdateAsync(comment);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating assessment comment");
            }
        }
    }
}