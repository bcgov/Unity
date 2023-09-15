using System;
using Unity.GrantManager.Comments;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IGrantApplicationAppService : ICrudAppService<
            GrantApplicationDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateGrantApplicationDto>, ICommentsService
    {
    }
}
