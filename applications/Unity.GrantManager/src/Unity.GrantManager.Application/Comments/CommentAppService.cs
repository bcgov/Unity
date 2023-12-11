using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Exceptions;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;

namespace Unity.GrantManager.Comments
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(CommentAppService), typeof(ICommentAppService))]
    public class CommentAppService : ApplicationService, ICommentAppService
    {
        private readonly ICommentsManager _commentsManager;

        public CommentAppService(ICommentsManager commentsManager)
        {
            _commentsManager = commentsManager;
        }

        public async Task<CommentDto> CreateAsync(CreateCommentByTypeDto dto)
        {
            return ObjectMapper.Map<CommentBase, CommentDto>
                (await _commentsManager.CreateCommentAsync(dto.OwnerId, dto.Comment, dto.CommentType));
        }

        public async Task<IReadOnlyList<CommentDto>> GetListAsync(QueryCommentsByTypeDto dto)
        {
            return ObjectMapper.Map<IReadOnlyList<CommentListItem>, IReadOnlyList<CommentDto>>
                (await _commentsManager.GetCommentsDisplayListAsync(dto.OwnerId, dto.CommentType));
        }

        public async Task<CommentDto> UpdateAsync(UpdateCommentByTypeDto dto)
        {
            try
            {
                return ObjectMapper.Map<CommentBase, CommentDto>
                    (await _commentsManager.UpdateCommentAsync(dto.OwnerId, dto.CommentId, dto.Comment, dto.CommentType));

            }
            catch (EntityNotFoundException)
            {
                throw new InvalidCommentParametersException();
            }
        }

        public async Task<CommentDto> GetAsync(Guid id, QueryCommentsByTypeDto dto)
        {
            var comment = await _commentsManager.GetCommentAsync(dto.OwnerId, id, dto.CommentType);

            return comment == null
                ? throw new InvalidCommentParametersException()
                : ObjectMapper.Map<CommentBase, CommentDto>(comment);
        }
    }
}