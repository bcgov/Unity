using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Unity.GrantManager.Comments.CommentsManager;

namespace Unity.GrantManager.Comments
{
    public interface ICommentsManager
    {        
        Task<CommentBase> CreateCommentAsync(Guid id, string comment, CommentType assessmentComment);
        Task<IReadOnlyList<CommentBase>> GetCommentsAsync(Guid id, CommentType type);
        Task<CommentBase> UpdateCommentAsync(Guid id, Guid commentId, string comment, CommentType type);
    }
}
