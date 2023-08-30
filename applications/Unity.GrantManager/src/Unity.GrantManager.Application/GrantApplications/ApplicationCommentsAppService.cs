using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.Applications;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ApplicationCommentsService), typeof(IApplicationCommentsService))]
    public class ApplicationCommentsService : ApplicationService, IApplicationCommentsService
    {
        private readonly IApplicationCommentsRepository _applicationCommentsRepository;

        public ApplicationCommentsService(IApplicationCommentsRepository applicationCommentsRepository)
        {
            _applicationCommentsRepository = applicationCommentsRepository;
        }

        public async Task<IList<ApplicationCommentDto>> GetListAsync(Guid applicationId)
        {
            IQueryable<ApplicationComment> queryableComment = _applicationCommentsRepository.GetQueryableAsync().Result;
            var comments = queryableComment.Where(c => c.ApplicationId.Equals(applicationId)).ToList();
            return await Task.FromResult<IList<ApplicationCommentDto>>(ObjectMapper.Map<List<ApplicationComment>, List<ApplicationCommentDto>>(comments.OrderByDescending(s => s.CreationTime).ToList()));
        }


        public async Task<ApplicationCommentDto> CreateApplicationComment(CreateApplicationCommentDto dto)
        {            
            try
            {
                return ObjectMapper.Map<ApplicationComment, ApplicationCommentDto>(
                    await _applicationCommentsRepository.InsertAsync(
                    new ApplicationComment
                    {
                        Comment = dto.Comment,
                        ApplicationId = dto.ApplicationId,
                    },
                    autoSave: true
                ));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating application comment");
                return new ApplicationCommentDto();
                // TODO: Exception handling
            }
        }

        public async Task UpdateApplicationComment(UpdateApplicationCommentDto dto)
        {
            try
            {
                var comment = await _applicationCommentsRepository.GetAsync(dto.CommentId);
                if (comment != null)
                {
                    comment.Comment = dto.Comment;
                    await _applicationCommentsRepository.UpdateAsync(comment);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating application comment");
            }
        }
    }
}
