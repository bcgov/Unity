using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Comments
{
    public interface ICommentsService
    {
        Task<CommentDto> CreateCommentAsync(Guid id, CreateCommentDto dto);
        Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid id);
        Task<CommentDto> UpdateCommentAsync(Guid id, UpdateCommentDto dto);
        Task<CommentDto> GetCommentAsync(Guid id, Guid commentId);
    }
}
