using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Unity.GrantManager.Comments
{
    public interface ICommentAppService
    {
        Task<CommentDto> CreateAsync(CreateCommentByTypeDto dto);
        Task<IReadOnlyList<CommentDto>> GetListAsync(QueryCommentsByTypeDto dto);        
        Task<CommentDto> UpdateAsync(UpdateCommentByTypeDto dto);
        Task<CommentDto> GetAsync(Guid id, QueryCommentsByTypeDto dto);
    }
}
