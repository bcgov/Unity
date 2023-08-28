using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications
{

    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(AssessmentCommentService), typeof(IAssessmentCommentService))]
    public class AssessmentCommentService : ApplicationService, IAssessmentCommentService
    {
        private readonly IAssessmentCommentRepository _assessmentCommentRepository;

        public AssessmentCommentService(IAssessmentCommentRepository repository)
        {
            _assessmentCommentRepository = repository;
        }

        public async Task<IList<AssessmentCommentDto>> GetListAsync(Guid applicationFormSubmissionId)
        {
            IQueryable<AssessmentComment> queryableComment = _assessmentCommentRepository.GetQueryableAsync().Result;
            var comments = queryableComment.Where(c => c.ApplicationFormSubmissionId.Equals(applicationFormSubmissionId)).ToList();
            return await Task.FromResult<IList<AssessmentCommentDto>>(ObjectMapper.Map<List<AssessmentComment>, List<AssessmentCommentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
        }


        public async Task<AssessmentComment> CreateAssessmentComment(string AssessmentComment, string applicationFormSubmissionId)
        {
            AssessmentComment newAssesment = new AssessmentComment();
            try
            {
                if (AssessmentComment != null)
                {
                    newAssesment = await _assessmentCommentRepository.InsertAsync(
                        new AssessmentComment
                        {
                            Comment = AssessmentComment,
                            ApplicationFormSubmissionId = Guid.Parse(applicationFormSubmissionId),
                        },
                        autoSave: true
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            return newAssesment;
        }

        public async Task UpdateAssessmentComment(string AssessmentCommentId, String AssessmentComment)
        {
            try
            {
                var comment = await _assessmentCommentRepository.GetAsync(Guid.Parse(AssessmentCommentId));
                if (comment != null)
                {
                    comment.Comment = AssessmentComment;
                    await _assessmentCommentRepository.UpdateAsync(comment);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}