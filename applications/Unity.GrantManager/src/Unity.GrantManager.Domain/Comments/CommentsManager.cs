using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Services;
using Volo.Abp.Users;

namespace Unity.GrantManager.Comments
{
    public class CommentsManager : DomainService, ICommentsManager
    {
        private readonly ICommentsRepository<ApplicationComment> _applicationCommentsRepository;
        private readonly ICommentsRepository<AssessmentComment> _assessmentCommentsRepository;
        private readonly ICurrentUser _currentUser;

        public CommentsManager(ICommentsRepository<ApplicationComment> applicationCommentsRepository,
            ICommentsRepository<AssessmentComment> assessmentCommentsRepository,
            ICurrentUser currentUser)
        {
            _applicationCommentsRepository = applicationCommentsRepository;
            _assessmentCommentsRepository = assessmentCommentsRepository;
            _currentUser = currentUser;
        }

        public async Task<CommentBase> CreateCommentAsync(Guid ownerId, string comment, CommentType assessmentComment)
        {
            return assessmentComment switch
            {
                CommentType.ApplicationComment => await _applicationCommentsRepository
                    .InsertAsync(new ApplicationComment() 
                        { Commenter = GetCommenter(), Comment = comment, ApplicationId = ownerId }, autoSave: true),
                CommentType.AssessmentComment => await _assessmentCommentsRepository
                    .InsertAsync(new AssessmentComment() 
                        { Commenter = GetCommenter(), Comment = comment, AssessmentId = ownerId }, autoSave: true),
                _ => throw new NotImplementedException(),
            };
        }

        private string GetCommenter()
        {
            return $"{_currentUser.SurName}, {_currentUser.Name}";
        }

        public async Task<IReadOnlyList<CommentBase>> GetCommentsAsync(Guid ownerId, CommentType type)
        {
            switch (type)
            {
                case CommentType.ApplicationComment:
                    var applicationCommentsQry = await _applicationCommentsRepository.GetQueryableAsync();                    
                    return applicationCommentsQry.Where(c => c.ApplicationId.Equals(ownerId)).OrderByDescending(s => s.CreationTime).ToList();
                case CommentType.AssessmentComment:
                    var assessmentCommentsQry = await _assessmentCommentsRepository.GetQueryableAsync();
                    return assessmentCommentsQry.Where(c => c.AssessmentId.Equals(ownerId)).OrderByDescending(s => s.CreationTime).ToList();
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<CommentBase> UpdateCommentAsync(Guid ownerId, Guid commentId, string comment, CommentType type)
        {
            switch (type)
            {
                case CommentType.ApplicationComment:
                    var applicationComment = await GetCommentAsync(ownerId, commentId, type) ?? throw new EntityNotFoundException();
                    applicationComment.Comment = comment;
                    return await _applicationCommentsRepository.UpdateAsync((ApplicationComment)applicationComment!, autoSave: true);
                case CommentType.AssessmentComment:
                    var assessmentComment = await GetCommentAsync(ownerId, commentId, type) ?? throw new EntityNotFoundException();
                    assessmentComment.Comment = comment;
                    return await _assessmentCommentsRepository.UpdateAsync((AssessmentComment)assessmentComment!, autoSave: true);
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<CommentBase?> GetCommentAsync(Guid ownerId, Guid commentId, CommentType type)
        {
            switch (type)
            {
                case CommentType.ApplicationComment:
                    var applicationCommentsQry = await _applicationCommentsRepository.GetQueryableAsync();
                    return applicationCommentsQry.FirstOrDefault(s => s.ApplicationId == ownerId && s.Id == commentId);
                case CommentType.AssessmentComment:
                    var assessmentCommentsQry = await _assessmentCommentsRepository.GetQueryableAsync();
                    return assessmentCommentsQry.FirstOrDefault(s => s.AssessmentId == ownerId && s.Id == commentId);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
