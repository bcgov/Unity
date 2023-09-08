using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Unity.GrantManager.Comments.CommentsManager;

namespace Unity.GrantManager.Comments
{
    public interface ICommentsManager
    {        
        Task<CommentBase> CreateCommentAsync(Guid ownerId, string comment, CommentType assessmentComment);
        Task<IReadOnlyList<CommentBase>> GetCommentsAsync(Guid ownerId, CommentType type);
        Task<CommentBase> UpdateCommentAsync(Guid ownerId, Guid commentId, string comment, CommentType type);
        Task<CommentBase?> GetCommentAsync(Guid ownerId, Guid commentId, CommentType type);
    }
}
