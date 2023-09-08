using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Comments
{
    public class CommentsManager : DomainService, ICommentsManager
    {
        public enum CommentType
        {
            ApplicationComment,
            AssessmentComment
        }

        private readonly ICommentsRepository<ApplicationComment> _applicationCommentsRepository;
        private readonly ICommentsRepository<AssessmentComment> _assessmentCommentsRepository;

        public CommentsManager(ICommentsRepository<ApplicationComment> applicationCommentsRepository,
            ICommentsRepository<AssessmentComment> assessmentCommentsRepository)
        {
            _applicationCommentsRepository = applicationCommentsRepository;
            _assessmentCommentsRepository = assessmentCommentsRepository;
        }

        public async Task<CommentBase> CreateCommentAsync(Guid id, string comment, CommentType assessmentComment)
        {
            return assessmentComment switch
            {
                CommentType.ApplicationComment => await _applicationCommentsRepository.InsertAsync(new ApplicationComment() { Comment = comment, ApplicationId = id }, autoSave: true),
                CommentType.AssessmentComment => await _assessmentCommentsRepository.InsertAsync(new AssessmentComment() { Comment = comment, AssessmentId = id }, autoSave: true),
                _ => throw new NotImplementedException(),
            };
        }

        public async Task<IReadOnlyList<CommentBase>> GetCommentsAsync(Guid id, CommentType type)
        {
            switch (type)
            {
                case CommentType.ApplicationComment:
                    IQueryable<ApplicationComment> applicationCommentsQry = await _applicationCommentsRepository.GetQueryableAsync();
                    return applicationCommentsQry.Where(c => c.ApplicationId.Equals(id)).OrderByDescending(s => s.CreationTime).ToList();
                case CommentType.AssessmentComment:
                    IQueryable<AssessmentComment> assessmentCommentsQry = await _assessmentCommentsRepository.GetQueryableAsync();
                    return assessmentCommentsQry.Where(c => c.AssessmentId.Equals(id)).OrderByDescending(s => s.CreationTime).ToList();
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<CommentBase> UpdateCommentAsync(Guid id, Guid commentId, string comment, CommentType type)
        {
            switch (type)
            {
                case CommentType.ApplicationComment:
                    var applicationComment = await _applicationCommentsRepository.GetAsync(commentId);
                    applicationComment.Comment = comment;
                    return await _applicationCommentsRepository.UpdateAsync(applicationComment, autoSave: true);                    
                case CommentType.AssessmentComment:
                    var assessmentComment = await _assessmentCommentsRepository.GetAsync(commentId);
                    assessmentComment.Comment = comment;
                    return await _assessmentCommentsRepository.UpdateAsync(assessmentComment, autoSave: true);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
