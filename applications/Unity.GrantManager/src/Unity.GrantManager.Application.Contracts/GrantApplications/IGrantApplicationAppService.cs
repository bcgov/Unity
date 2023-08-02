using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IGrantApplicationAppService : ICrudAppService<
            GrantApplicationDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateGrantApplicationDto>
    {
    }
}
