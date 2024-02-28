using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
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
        private readonly IPersonRepository _personRepository;

        public CommentsManager(ICommentsRepository<ApplicationComment> applicationCommentsRepository,
            ICommentsRepository<AssessmentComment> assessmentCommentsRepository,
            ICurrentUser currentUser,
            IPersonRepository personRepository)
        {
            _applicationCommentsRepository = applicationCommentsRepository;
            _assessmentCommentsRepository = assessmentCommentsRepository;
            _currentUser = currentUser;
            _personRepository = personRepository;
        }

        public async Task<CommentBase> CreateCommentAsync(Guid ownerId, string comment, CommentType assessmentComment)
        {
            Guid commenterId = _currentUser.GetId();

            return assessmentComment switch
            {
                CommentType.ApplicationComment => await _applicationCommentsRepository
                    .InsertAsync(new ApplicationComment()
                    { Comment = comment, ApplicationId = ownerId, CommenterId = commenterId }, autoSave: true),
                CommentType.AssessmentComment => await _assessmentCommentsRepository
                    .InsertAsync(new AssessmentComment()
                    { Comment = comment, AssessmentId = ownerId, CommenterId = commenterId }, autoSave: true),
                _ => throw new NotImplementedException(),
            };
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

        public async Task<IReadOnlyList<CommentListItem>> GetCommentsDisplayListAsync(Guid ownerId, CommentType type)
        {
            switch (type)
            {
                case CommentType.ApplicationComment:
                    var applicationCommentsQry = from applicationComment in await _applicationCommentsRepository.GetQueryableAsync()
                                                 join user in await _personRepository.GetQueryableAsync() on applicationComment.CommenterId equals user.Id
                                                 where applicationComment.ApplicationId == ownerId
                                                 orderby applicationComment.CreationTime descending
                                                 select new CommentListItem
                                                 {
                                                     Comment = applicationComment.Comment,
                                                     CommenterId = applicationComment.CommenterId,
                                                     CommenterDisplayName = user.OidcDisplayName,
                                                     CommenterBadge = user.Badge,
                                                     CreationTime = applicationComment.CreationTime,
                                                     OwnerId = ownerId,
                                                     Id = applicationComment.Id,
                                                     LastModificationTime = applicationComment.LastModificationTime,
                                                 };
                    return applicationCommentsQry.ToList();
                case CommentType.AssessmentComment:
                    var assessmentCommentsQry = from assessmentComment in await _assessmentCommentsRepository.GetQueryableAsync()
                                                join user in await _personRepository.GetQueryableAsync() on assessmentComment.CommenterId equals user.Id
                                                where assessmentComment.AssessmentId == ownerId
                                                orderby assessmentComment.CreationTime descending
                                                select new CommentListItem
                                                {
                                                    Comment = assessmentComment.Comment,
                                                    CommenterId = assessmentComment.CommenterId,
                                                    CommenterDisplayName = user.OidcDisplayName,
                                                    CommenterBadge = user.Badge,
                                                    CreationTime = assessmentComment.CreationTime,
                                                    OwnerId = ownerId,
                                                    Id = assessmentComment.Id,
                                                    LastModificationTime = assessmentComment.LastModificationTime,
                                                };
                    return assessmentCommentsQry.ToList();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
